using Godot;
using System;
using System.Threading.Tasks;
using RLMatrix;
using OneOf;

public partial class BallBalanceEnv : Node3D, IContinuousEnvironmentAsync<float[]>
{
    [Export] public RigidBody3D Ball { get; set; }
    [Export] public RigidBody3D Head { get; set; }
    [Export] public float HeadRadius { get; set; } = 5f;

    private int poolingRate = 1;
    private RLMatrixPoolingHelper poolingHelper;
    private int stepsSoft = 0;
    private int stepsHard = 0;

    private int _maxStepsHard = 5000;
    private int maxStepsHard => _maxStepsHard / poolingRate;

    private int _maxStepsSoft = 1000;
    private int maxStepsSoft => _maxStepsSoft / poolingRate;

    public OneOf<int, (int, int)> StateSize { get; set; }
    public int[] DiscreteActionSize { get; set; } = Array.Empty<int>();
    public (float min, float max)[] ContinuousActionBounds { get; set; } = new[]
    {
        (-1f, 1f),
        (-1f, 1f),
    };

    private bool isDone;
    private const float MaxAngularVelocity = 3f;

    public void Initialize(int poolingRate = 1)
    {
        this.poolingRate = poolingRate;
        poolingHelper = new RLMatrixPoolingHelper(poolingRate, ContinuousActionBounds.Length, GetObservations);
        StateSize = poolingRate * 8;
        isDone = true;
        InitializeObservations();
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
        ResetEnvironment();
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

        ApplyActions(continuousActions);

        float stepReward = CalculateReward();
        poolingHelper.CollectObservation(stepReward);

        float totalReward = poolingHelper.GetAndResetAccumulatedReward();
        isDone = IsHardDone() || IsSoftDone();

        poolingHelper.SetAction(continuousActions);

        return Task.FromResult((totalReward, isDone));
    }

    private bool IsHardDone() => stepsHard >= maxStepsHard || IsDone();

    private bool IsSoftDone() => stepsSoft >= maxStepsSoft;

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

    private float[] GetObservations()
    {
        return new float[]
        {
            Head.Rotation.X,
            Head.Rotation.Z,
            BallOffsetObservation().X / HeadRadius,
            BallOffsetObservation().Y / HeadRadius,
            BallOffsetObservation().Z / HeadRadius,
            Ball.LinearVelocity.X / 10f,
            Ball.LinearVelocity.Y / 10f,
            Ball.LinearVelocity.Z / 10f
        };
    }

    float modifier = 0.2f;
    private void ApplyActions(float[] actions)
    {
        Vector3 angularVelocity = new Vector3(
            actions[0] * modifier* MaxAngularVelocity,
            0,
            actions[1] * modifier* MaxAngularVelocity
        );
        Head.AngularVelocity = angularVelocity;
    }

    private Vector3 BallOffsetObservation() => Ball.GlobalPosition - Head.GlobalPosition;

    private float CalculateReward()
    {
        Vector3 ballOffset = BallOffsetObservation();
        if (ballOffset.Y < -2f || Mathf.Abs(ballOffset.X) > HeadRadius || Mathf.Abs(ballOffset.Z) > HeadRadius)
        {
            return -1f;
        }
        return 1f;
    }

    private bool IsDone()
    {
        Vector3 ballOffset = BallOffsetObservation();
        return ballOffset.Y < -2f || Mathf.Abs(ballOffset.X) > HeadRadius || Mathf.Abs(ballOffset.Z) > HeadRadius;
    }

    private void ResetEnvironment()
    {
        Head.Rotation = new Vector3(
            Mathf.DegToRad((float)GD.RandRange(-10.0, 10.0)),
            0,
            Mathf.DegToRad((float)GD.RandRange(-10.0, 10.0))
        );
        Head.AngularVelocity = Vector3.Zero;
        Ball.LinearVelocity = Vector3.Zero;
        Ball.AngularVelocity = Vector3.Zero;
    
        float spawnRadius = HeadRadius * 0.15f;
        Vector3 randomOffset = new Vector3(
            (float)GD.RandRange(-spawnRadius, spawnRadius),
            HeadRadius * 0.3f,
            (float)GD.RandRange(-spawnRadius, spawnRadius)
        );
        Ball.GlobalPosition = Head.GlobalPosition + randomOffset;
    }
}







using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using RLMatrix;
using RLMatrix.Agents.Common;
using RLMatrix.Common.Remote;
using System.Threading.Tasks;
using RLMatrix.Agents.SignalR;

public partial class BallBalanceTrainingManager : Node
{
    [Export] private int poolingRate = 5;
    [Export] private float timeScale = 1f;
    [Export] private float stepInterval = 0.02f;

    private PPOAgentOptions optsppo = new PPOAgentOptions(
        batchSize: 64,
        memorySize: 10000,
        gamma: 0.99f,
        gaeLambda: 0.95f,
        lr: 1e-3f,
        width: 128,
        depth: 2,
        clipEpsilon: 0.2f,
        vClipRange: 0.2f,
        cValue: 0.5f,
        ppoEpochs: 10,
        clipGradNorm: 0.5f,
        entropyCoefficient: 0.0005f,
        useRNN: false
    );

    //local
    private LocalContinuousRolloutAgent<float[]> myAgent; 
   //remote
   //private RemoteContinuousRolloutAgent<float[]> myAgent;
    private List<BallBalanceEnv> myEnvs;
    private int stepCounter = 0;
    private float accumulatedTime = 0f;
    private int stepsTooSlowInRow = 0;

    public override void _Ready()
    {

        
        Engine.TimeScale = timeScale;
        myEnvs = GetAllChildrenOfType<BallBalanceEnv>(this).ToList();

        if (myEnvs.Count == 0)
        {
            GD.PrintErr("No BallBalanceEnv nodes found in children.");
            return;
        }

        InitializeEnvironments();

        GD.Print($"Found {myEnvs.Count} environments with pooling rate {poolingRate}.");

        _ = InitializeAgent();
    }

    private List<T> GetAllChildrenOfType<T>(Node parentNode) where T : class
    {
        List<T> resultList = new List<T>();
        AddChildrenOfType(parentNode, resultList);
        return resultList;
    }

    private void AddChildrenOfType<T>(Node node, List<T> resultList) where T : class
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T typedChild)
            {
                resultList.Add(typedChild);
            }
            AddChildrenOfType(child, resultList);
        }
    }

    private void InitializeEnvironments()
    {
        foreach (var env in myEnvs)
        {
            env.Initialize(poolingRate);
        }
    }

    private async Task InitializeAgent()
    {
        await Task.Run(() =>
        {
            //local
            myAgent = new LocalContinuousRolloutAgent<float[]>(optsppo, myEnvs);
            
            //remote
          //  myAgent = new RemoteContinuousRolloutAgent<float[]>("http://127.0.0.1:5006/rlmatrixhub", optsppo, myEnvs);
        });

        Engine.TimeScale = timeScale;
    }

    public override void _Process(double delta)
    {
        if (myAgent == null) return;

        accumulatedTime += (float)delta;

        while (accumulatedTime >= stepInterval / Engine.TimeScale)
        {
            /*
            if(accumulatedTime > 2 * stepInterval)
            {
                stepsTooSlowInRow++;

                if(stepsTooSlowInRow > 10)
                {
                    GD.PrintErr("Too slow, throttling.");
                    stepsTooSlowInRow = 0;
                    timeScale *= 0.9f;
                    Engine.TimeScale = timeScale;
                }
            }
            else
            {
                stepsTooSlowInRow = 0;
            }
            */
            
            PerformStep();
            accumulatedTime -= stepInterval / (float)Engine.TimeScale;
        }
    }

    private void PerformStep()
    {
        if (stepCounter % poolingRate == poolingRate - 1)
        {
            //pause time
            var cacheScale = Engine.TimeScale;
            Engine.TimeScale = 0f;
            // Actual agent-env step
            myAgent.StepSync(true);
            Engine.TimeScale = cacheScale;
        }
        else
        {
            // Ghost step
            foreach (var env in myEnvs)
            {
                env.GhostStep();
            }
        }
        stepCounter = (stepCounter + 1) % poolingRate;
    }
}







using System;
using System.Collections.Generic;

public class RLMatrixPoolingHelper
{
    private int poolingRate;
    private Queue<float[]> observationBuffer;
    private float[] lastAction;
    private Func<float[]> getObservationFunc;
    private int singleObservationSize;
    private float accumulatedReward;

    public bool HasAction { get; private set; }

    public RLMatrixPoolingHelper(int rate, int actionSize, Func<float[]> getObservation)
    {
        poolingRate = rate;
        lastAction = new float[actionSize];
        getObservationFunc = getObservation;
        HasAction = false;
        singleObservationSize = getObservation().Length;
        accumulatedReward = 0f;
        observationBuffer = new Queue<float[]>(poolingRate);
        InitializeObservations();
    }

    private void InitializeObservations()
    {
        for (int i = 0; i < poolingRate; i++)
        {
            CollectObservation(0f);
        }
    }

    public void SetAction(float[] action)
    {
        Array.Copy(action, lastAction, action.Length);
        HasAction = true;
    }

    public float[] GetLastAction()
    {
        return lastAction;
    }

    public void CollectObservation(float reward)
    {
        float[] currentObservation = getObservationFunc();
        if (observationBuffer.Count >= poolingRate)
        {
            observationBuffer.Dequeue();
        }
        observationBuffer.Enqueue(currentObservation);
        accumulatedReward += reward;
    }

    public float[] GetPooledObservations()
    {
        float[] pooledObservations = new float[singleObservationSize * poolingRate];
        int index = 0;
        foreach (var observation in observationBuffer)
        {
            Array.Copy(observation, 0, pooledObservations, index, singleObservationSize);
            index += singleObservationSize;
        }
        return pooledObservations;
    }

    public float GetAndResetAccumulatedReward()
    {
        float reward = accumulatedReward;
        accumulatedReward = 0f;
        return reward;
    }

    public void Reset()
    {
        observationBuffer.Clear();
        HasAction = false;
        accumulatedReward = 0f;
        InitializeObservations();
    }
    public void HardReset(Func<float[]> getInitialObservation)
    {
        observationBuffer.Clear();
        HasAction = false;
        accumulatedReward = 0f;
        lastAction = new float[lastAction.Length];  // Reset the last action

        // Fill the buffer with new observations
        for (int i = 0; i < poolingRate; i++)
        {
            float[] newObservation = getInitialObservation();
            observationBuffer.Enqueue(newObservation);
        }
    }
}