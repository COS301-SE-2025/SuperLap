using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using RLMatrix;
using RLMatrix.Agents.Common;
using System.Threading.Tasks;

public class MotorcycleTrainingManager : MonoBehaviour
{
    [Header("Training Settings")]
    [SerializeField] private int poolingRate = 5;
    [SerializeField] private float timeScale = 1f;
    [SerializeField] private float stepInterval = 0.02f;

    [Header("PPO Agent Configuration")]
    [SerializeField] private int batchSize = 64;
    [SerializeField] private int memorySize = 10000;
    [SerializeField] private float gamma = 0.99f;
    [SerializeField] private float gaeLambda = 0.95f;
    [SerializeField] private float learningRate = 1e-1f;
    [SerializeField] private int networkWidth = 128;
    [SerializeField] private int networkDepth = 2;
    [SerializeField] private float clipEpsilon = 0.2f;
    [SerializeField] private float vClipRange = 0.2f;
    [SerializeField] private float cValue = 0.5f;
    [SerializeField] private int ppoEpochs = 10;
    [SerializeField] private float clipGradNorm = 0.5f;
    [SerializeField] private float entropyCoefficient = 0.0005f;
    [SerializeField] private bool useRNN = false;

    [Header("Training Progress")]
    [SerializeField] private int episodeCount = 0;
    [SerializeField] private float averageReward = 0f;
    [SerializeField] private float bestEpisodeReward = float.MinValue;
    [SerializeField] private int totalSteps = 0;

    // Training components
    private PPOAgentOptions ppoOptions;
    private LocalDiscreteRolloutAgent<float[]> localAgent;
    private List<MotorcycleEnvironment> environments;
    
    // Step management
    private int stepCounter = 0;
    private float accumulatedTime = 0f;
    private int stepsTooSlowInRow = 0;
    
    // Training state
    private bool isInitialized = false;
    private bool isTraining = false;

    public void Initialize(List<MotorcycleEnvironment> envs)
    {
        environments = envs;
        
        if (environments == null || environments.Count == 0)
        {
            Debug.LogError("MotorcycleTrainingManager: No environments provided!");
            return;
        }

        // Initialize environments
        foreach (var env in environments)
        {
            env.Initialize(poolingRate);
        }

        Debug.Log($"MotorcycleTrainingManager: Found {environments.Count} environments with pooling rate {poolingRate}.");

        // Initialize agent asynchronously
        _ = InitializeAgent();
    }

    private async Task InitializeAgent()
    {
        await Task.Run(() =>
        {
            // Create PPO options
            ppoOptions = new PPOAgentOptions(
                batchSize: batchSize,
                memorySize: memorySize,
                gamma: gamma,
                gaeLambda: gaeLambda,
                lr: learningRate,
                width: networkWidth,
                depth: networkDepth,
                clipEpsilon: clipEpsilon,
                vClipRange: vClipRange,
                cValue: cValue,
                ppoEpochs: ppoEpochs,
                clipGradNorm: clipGradNorm,
                entropyCoefficient: entropyCoefficient,
                useRNN: useRNN
            );

            Debug.Log("Initializing Local RLMatrix Agent...");
            localAgent = new LocalDiscreteRolloutAgent<float[]>(ppoOptions, environments);
        });

        // Set time scale and mark as initialized
        Time.timeScale = timeScale;
        isInitialized = true;
        isTraining = true;
        
        Debug.Log("MotorcycleTrainingManager: Agent initialized successfully!");
    }

    void Update()
    {
        if (!isInitialized || !isTraining) return;

        accumulatedTime += Time.deltaTime;

        while (accumulatedTime >= stepInterval / Time.timeScale)
        {
            // Optional: Performance monitoring
            /*
            if(accumulatedTime > 2 * stepInterval)
            {
                stepsTooSlowInRow++;

                if(stepsTooSlowInRow > 10)
                {
                    Debug.LogWarning("Training too slow, reducing time scale.");
                    stepsTooSlowInRow = 0;
                    timeScale *= 0.9f;
                    Time.timeScale = timeScale;
                }
            }
            else
            {
                stepsTooSlowInRow = 0;
            }
            */
            
            PerformStep();
            accumulatedTime -= stepInterval / Time.timeScale;
        }
    }

    private void PerformStep()
    {
        totalSteps++;
        
        if (stepCounter % poolingRate == poolingRate - 1)
        {
            // Pause time for agent step
            var cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            
            // Actual agent-environment step
            localAgent.Step(true);
            
            // Restore time scale
            Time.timeScale = cachedTimeScale;
        }
        else
        {
            // Ghost step for all environments
            foreach (var env in environments)
            {
                env.GhostStep();
            }
        }
        
        stepCounter = (stepCounter + 1) % poolingRate;
        
        // Log progress periodically
        if (totalSteps % 1000 == 0)
        {
            LogTrainingProgress();
        }
    }

    private void LogTrainingProgress()
    {
        // Calculate average reward from recent episodes
        float totalReward = 0f;
        int activeEnvironments = 0;
        
        foreach (var env in environments)
        {
            if (env != null && env.GetMotorcycleAgent() != null)
            {
                totalReward += env.GetMotorcycleAgent().GetCumulativeReward();
                activeEnvironments++;
            }
        }
        
        if (activeEnvironments > 0)
        {
            averageReward = totalReward / activeEnvironments;
        }

        Debug.Log($"Training Progress - Steps: {totalSteps}, Avg Reward: {averageReward:F2}, Environments: {activeEnvironments}");
    }

    public void PauseTraining()
    {
        isTraining = false;
        Debug.Log("Training paused.");
    }

    public void ResumeTraining()
    {
        if (isInitialized)
        {
            isTraining = true;
            Debug.Log("Training resumed.");
        }
    }

    public void StopTraining()
    {
        isTraining = false;
        
        // Cleanup agents
        if (localAgent != null)
        {
            // Local agent cleanup if needed
        }
        
        Debug.Log("Training stopped.");
    }

    // Public getters for monitoring
    public bool IsInitialized => isInitialized;
    public bool IsTraining => isTraining;
    public int EpisodeCount => episodeCount;
    public float AverageReward => averageReward;
    public float BestEpisodeReward => bestEpisodeReward;
    public int TotalSteps => totalSteps;
    public int EnvironmentCount => environments?.Count ?? 0;

    void OnDestroy()
    {
        StopTraining();
    }

    // Helper method to find all motorcycle environments in children
    private List<T> GetAllChildrenOfType<T>(Transform parentTransform) where T : class
    {
        List<T> resultList = new List<T>();
        AddChildrenOfType(parentTransform, resultList);
        return resultList;
    }

    private void AddChildrenOfType<T>(Transform transform, List<T> resultList) where T : class
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<T>(out T component))
            {
                resultList.Add(component);
            }
            AddChildrenOfType(child, resultList);
        }
    }
}
