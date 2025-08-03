using UnityEngine;
using System;
using System.Threading.Tasks;
using RLMatrix;
using OneOf;
using System.Collections.Generic;

public class MotorcycleEnvironment : MonoBehaviour, IContinuousEnvironmentAsync<float[]>
{
    [Header("Environment Settings")]
    [SerializeField] private int poolingRate = 1;
    [SerializeField] private int maxStepsHard = 5000;
    [SerializeField] private int maxStepsSoft = 1000;
    
    [Header("Reward Settings")]
    [SerializeField] private float onTrackReward = 0.1f;
    [SerializeField] private float offTrackPenalty = -1.0f;
    [SerializeField] private float speedRewardScale = 0.02f;
    [SerializeField] private float racelineDeviationPenalty = 0.1f;
    [SerializeField] private float crashPenalty = -10.0f;
    
    // RLMatrix Interface Properties
    public OneOf<int, (int, int)> StateSize { get; set; }
    public int[] DiscreteActionSize { get; set; } = Array.Empty<int>();
    public (float min, float max)[] ContinuousActionBounds { get; set; } = new[]
    {
        (-1f, 1f), // Throttle/Brake
        (-1f, 1f), // Steering
    };

    // Internal state
    private RLMatrixPoolingHelper poolingHelper;
    private MotorcycleRLMatrixAgent motorcycleAgent;
    private int stepsSoft = 0;
    private int stepsHard = 0;
    private bool isDone;
    
    // Computed properties
    private int maxStepsHardComputed => maxStepsHard / poolingRate;
    private int maxStepsSoftComputed => maxStepsSoft / poolingRate;

    public void Initialize(int poolingRate = 1)
    {
        this.poolingRate = poolingRate;
        motorcycleAgent = GetComponent<MotorcycleRLMatrixAgent>();
        
        if (motorcycleAgent == null)
        {
            Debug.LogError("MotorcycleEnvironment requires a MotorcycleRLMatrixAgent component!");
            return;
        }

        poolingHelper = new RLMatrixPoolingHelper(poolingRate, ContinuousActionBounds.Length, GetObservations);
        StateSize = poolingRate * GetObservationSize();
        isDone = true;
        
        InitializeObservations();
        
        Debug.Log($"MotorcycleEnvironment initialized with pooling rate {poolingRate}, state size: {StateSize}");
    }

    private void InitializeObservations()
    {
        for (int i = 0; i < poolingRate; i++)
        {
            float reward = CalculateReward();
            poolingHelper.CollectObservation(reward);
        }
    }

    public Task<float[]> GetCurrentState()
    {
        if (isDone && IsHardDone())
        {
            Reset();
            poolingHelper.HardReset(GetObservations);
            isDone = false;
        }
        else if (isDone && IsSoftDone())
        {
            stepsSoft = 0;
            isDone = false;
        }

        return Task.FromResult(poolingHelper.GetPooledObservations());
    }

    public Task Reset()
    {
        stepsSoft = 0;
        stepsHard = 0;
        
        // Reset the motorcycle agent
        if (motorcycleAgent != null)
        {
            motorcycleAgent.ResetEpisode();
        }
        
        isDone = false;
        poolingHelper.HardReset(GetObservations);

        if (IsDone())
        {
            throw new Exception("Done flag still raised after reset - did you intend to reset?");
        }

        return Task.CompletedTask;
    }

    public Task<(float reward, bool done)> Step(int[] discreteActions, float[] continuousActions)
    {
        stepsSoft++;
        stepsHard++;

        // Apply actions to the motorcycle agent
        ApplyActions(continuousActions);

        float stepReward = CalculateReward();
        poolingHelper.CollectObservation(stepReward);

        float totalReward = poolingHelper.GetAndResetAccumulatedReward();
        isDone = IsHardDone() || IsSoftDone();

        poolingHelper.SetAction(continuousActions);

        return Task.FromResult((totalReward, isDone));
    }

    public void GhostStep()
    {
        if (IsHardDone() || IsSoftDone())
            return;

        if (poolingHelper.HasAction)
        {
            ApplyActions(poolingHelper.GetLastAction());
        }
        
        float reward = CalculateReward();
        poolingHelper.CollectObservation(reward);
    }

    private bool IsHardDone() => stepsHard >= maxStepsHardComputed || IsDone();
    private bool IsSoftDone() => stepsSoft >= maxStepsSoftComputed;

    private void ApplyActions(float[] actions)
    {
        if (motorcycleAgent != null && actions != null && actions.Length >= 2)
        {
            // Safety check for NaN values in actions
            float throttle = actions[0];
            float steering = actions[1];
            
            if (float.IsNaN(throttle) || float.IsInfinity(throttle))
            {
                throttle = 0f;
                Debug.LogWarning("NaN/Infinity detected in throttle action, replaced with 0");
            }
            
            if (float.IsNaN(steering) || float.IsInfinity(steering))
            {
                steering = 0f;
                Debug.LogWarning("NaN/Infinity detected in steering action, replaced with 0");
            }
            
            motorcycleAgent.TakeAction(throttle, steering);
        }
    }

    private float[] GetObservations()
    {
        if (motorcycleAgent == null)
        {
            return new float[GetObservationSize()];
        }

        // Collect all observations from the motorcycle agent
        float[] observations = new float[]
        {
            motorcycleAgent.GetNormalizedSpeed(),
            motorcycleAgent.GetNormalizedAcceleration(),
            motorcycleAgent.GetNormalizedTurnAngle(),
            motorcycleAgent.GetTrackDetection(),
            motorcycleAgent.GetNormalizedRacelineDeviation(),
            motorcycleAgent.GetEpisodeProgress(),
            motorcycleAgent.GetRecommendSpeedUp(),
            motorcycleAgent.GetRecommendSlowDown(),
            motorcycleAgent.GetRecommendTurnLeft(),
            motorcycleAgent.GetRecommendTurnRight()
        };

        // Safety check for NaN values in observations
        for (int i = 0; i < observations.Length; i++)
        {
            if (float.IsNaN(observations[i]) || float.IsInfinity(observations[i]))
            {
                observations[i] = 0f;
                Debug.LogWarning($"NaN/Infinity detected in observation {i}, replaced with 0");
            }
        }

        return observations;
    }

    private int GetObservationSize()
    {
        return 10; // Number of observations from GetObservations()
    }

    private float CalculateReward()
    {
        if (motorcycleAgent == null)
            return 0f;

        return motorcycleAgent.CalculateReward();
    }

    private bool IsDone()
    {
        if (motorcycleAgent == null)
            return true;

        return motorcycleAgent.IsEpisodeDone();
    }

    // Public methods for external access
    public MotorcycleRLMatrixAgent GetMotorcycleAgent() => motorcycleAgent;
    public int GetStepsSoft() => stepsSoft;
    public int GetStepsHard() => stepsHard;
    public bool GetIsDone() => isDone;
}
