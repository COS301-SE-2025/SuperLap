using System;
using System.Collections.Generic;

/// <summary>
/// Lightweight agent implementation for multithreaded training
/// Replaces the Unity MonoBehaviour-based MotorcycleAgent
/// Pure C# with no Unity dependencies
/// </summary>
public class LightweightAgent
{
    public ThreadedAgentState state;
    public readonly MotorcycleConfig config;
    public readonly AgentDecisionMaker decisionMaker;
    
    // Performance tracking
    private float totalSimulationTime;
    private int totalSteps;
    private Vector2D lastPosition;
    
    public LightweightAgent(int agentId, Vector2D startPosition, Vector2D startDirection, MotorcycleConfig config)
    {
        this.config = config;
        this.state = new ThreadedAgentState(agentId, startPosition, startDirection);
        this.decisionMaker = new AgentDecisionMaker(config);
        this.lastPosition = startPosition;
        this.totalSimulationTime = 0f;
        this.totalSteps = 0;
    }
    
    /// <summary>
    /// Perform one simulation step
    /// Updates physics, checks track bounds, and records metrics
    /// </summary>
    public void Step(float deltaTime, TrackMeshSystem track, DistanceCheckpointSystem checkpoints)
    {
        if (!state.isActive)
            return;
        
        // Store previous position for distance calculation
        Vector2D previousPosition = state.position;
        
        // Apply physics simulation
        PurePhysicsEngine.SimulateStep(ref state.position, ref state.direction, ref state.speed, 
                                     ref state.turnAngle, deltaTime, state.lastInput, config);
        
        // Update tracking metrics
        UpdateMetrics(deltaTime, previousPosition, track);
        
        // Check checkpoint progress
        int triggeredCheckpoint = checkpoints.UpdateAgentProgress(state.agentId, state.position);
        if (triggeredCheckpoint >= 0)
        {
            OnCheckpointTriggered(triggeredCheckpoint, checkpoints);
        }
        
        // Update total simulation time
        state.timeAlive += deltaTime;
        totalSimulationTime += deltaTime;
        totalSteps++;
    }
    
    /// <summary>
    /// Make a decision for the next action
    /// Uses the decision maker to choose throttle and steering inputs
    /// </summary>
    public AgentInput Decide(TrackMeshSystem track, DistanceCheckpointSystem checkpoints)
    {
        if (!state.isActive)
            return AgentInput.Zero;
        
        AgentInput decision = decisionMaker.MakeDecision(state, track, checkpoints);
        state.lastInput = decision;
        return decision;
    }
    
    /// <summary>
    /// Check if the agent is currently off the track
    /// </summary>
    public bool IsOffTrack(TrackMeshSystem track)
    {
        return !track.IsPointOnTrack(state.position);
    }
    
    /// <summary>
    /// Reset the agent to a new starting position and state
    /// </summary>
    public void Reset(Vector2D newPosition, Vector2D newDirection)
    {
        state.Reset(newPosition, newDirection);
        lastPosition = newPosition;
        totalSimulationTime = 0f;
        totalSteps = 0;
        decisionMaker.Reset();
    }
    
    /// <summary>
    /// Set the agent's input directly (for training scenarios)
    /// </summary>
    public void SetInput(AgentInput input)
    {
        state.lastInput = input;
    }
    
    /// <summary>
    /// Get performance metrics for this agent
    /// </summary>
    public AgentPerformanceMetrics GetPerformanceMetrics()
    {
        return new AgentPerformanceMetrics
        {
            agentId = state.agentId,
            totalSimulationTime = totalSimulationTime,
            totalSteps = totalSteps,
            averageSpeed = state.averageSpeed,
            maxSpeed = state.maxSpeed,
            totalDistance = state.totalDistance,
            timeOffTrack = state.timeOffTrack,
            offTrackCount = state.offTrackCount,
            checkpointsCompleted = state.checkpointsCompleted,
            stepsPerSecond = totalSteps / Math.Max(totalSimulationTime, 0.001f)
        };
    }
    
    #region Private Methods
    
    private void UpdateMetrics(float deltaTime, Vector2D previousPosition, TrackMeshSystem track)
    {
        // Update distance traveled
        float stepDistance = Vector2D.Distance(previousPosition, state.position);
        state.totalDistance += stepDistance;
        
        // Update speed metrics
        state.maxSpeed = Math.Max(state.maxSpeed, state.speed);
        
        // Update average speed (running average)
        if (totalSteps > 0)
        {
            state.averageSpeed = (state.averageSpeed * (totalSteps - 1) + state.speed) / totalSteps;
        }
        else
        {
            state.averageSpeed = state.speed;
        }
        
        // Track off-track time
        if (!track.IsPointOnTrack(state.position))
        {
            state.timeOffTrack += deltaTime;
            
            // Count discrete off-track events
            if (track.IsPointOnTrack(previousPosition))
            {
                state.offTrackCount++;
            }
        }
    }
    
    private void OnCheckpointTriggered(int checkpointId, DistanceCheckpointSystem checkpoints)
    {
        state.checkpointsCompleted++;
        state.currentTargetCheckpoint = checkpoints.GetNextTargetCheckpoint(state.agentId);
        
        // Check if lap completed
        if (state.checkpointsCompleted >= checkpoints.TotalCheckpoints)
        {
            state.hasCompletedLap = true;
        }
    }
    
    #endregion
}

/// <summary>
/// Decision making system for agents
/// Can be configured for different AI strategies or human-like behavior
/// </summary>
public class AgentDecisionMaker
{
    private readonly MotorcycleConfig config;
    private readonly Random random;
    private float explorationRate = 0.1f; // Probability of random action
    private int consecutiveDecisions = 0;
    
    // Decision history for learning
    private Queue<AgentInput> recentInputs;
    private const int InputHistorySize = 10;
    
    public AgentDecisionMaker(MotorcycleConfig config, int? randomSeed = null)
    {
        this.config = config;
        this.random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
        this.recentInputs = new Queue<AgentInput>();
    }
    
    /// <summary>
    /// Make a decision based on current agent state and environment
    /// </summary>
    public AgentInput MakeDecision(ThreadedAgentState state, TrackMeshSystem track, DistanceCheckpointSystem checkpoints)
    {
        AgentInput decision;
        
        // Use different strategies based on training phase or agent type
        if (ShouldExplore())
        {
            decision = MakeRandomDecision();
        }
        else
        {
            decision = MakeInformedDecision(state, track, checkpoints);
        }
        
        // Record decision in history
        RecordDecision(decision);
        
        return decision;
    }
    
    /// <summary>
    /// Reset decision maker state
    /// </summary>
    public void Reset()
    {
        recentInputs.Clear();
        consecutiveDecisions = 0;
    }
    
    /// <summary>
    /// Set exploration rate for random actions
    /// </summary>
    public void SetExplorationRate(float rate)
    {
        explorationRate = MathUtils.Clamp01(rate);
    }
    
    #region Private Decision Methods
    
    private bool ShouldExplore()
    {
        return random.NextDouble() < explorationRate;
    }
    
    private AgentInput MakeRandomDecision()
    {
        int throttle = random.Next(-1, 2); // -1, 0, or 1
        int steering = random.Next(-1, 2); // -1, 0, or 1
        return new AgentInput(throttle, steering);
    }
    
    private AgentInput MakeInformedDecision(ThreadedAgentState state, TrackMeshSystem track, DistanceCheckpointSystem checkpoints)
    {
        // Simple rule-based decision making
        // In a real implementation, this would be replaced with trained neural networks
        
        int throttle = DecideThrottle(state, track);
        int steering = DecideSteering(state, track, checkpoints);
        
        return new AgentInput(throttle, steering);
    }
    
    private int DecideThrottle(ThreadedAgentState state, TrackMeshSystem track)
    {
        // Check if trajectory will go off track with current speed
        var testInput = new AgentInput(1, 0); // Test accelerating
        bool willGoOffTrack = track.DoesTrajectoryGoOffTrack(
            state.position, state.direction, state.speed, state.turnAngle,
            testInput, 2.0f, 8, 0.25f, config);
        
        if (willGoOffTrack)
        {
            return -1; // Brake
        }
        
        // Simple speed management
        float theoreticalTopSpeed = PurePhysicsEngine.CalculateTheoreticalTopSpeed(config);
        if (state.speed < theoreticalTopSpeed * 0.8f)
        {
            return 1; // Accelerate
        }
        
        return 0; // Coast
    }
    
    private int DecideSteering(ThreadedAgentState state, TrackMeshSystem track, DistanceCheckpointSystem checkpoints)
    {
        // Get target checkpoint position
        int targetCheckpoint = checkpoints.GetNextTargetCheckpoint(state.agentId);
        Vector2D checkpointPosition = checkpoints.GetCheckpointPosition(targetCheckpoint);
        
        // Calculate direction to checkpoint
        Vector2D toCheckpoint = (checkpointPosition - state.position).Normalized;
        
        // Calculate cross product to determine turn direction
        float cross = state.direction.x * toCheckpoint.y - state.direction.y * toCheckpoint.x;
        
        // Apply steering based on direction to checkpoint
        if (Math.Abs(cross) > 0.1f) // Threshold to avoid jittery steering
        {
            return cross > 0 ? 1 : -1; // Turn right or left
        }
        
        return 0; // Go straight
    }
    
    private void RecordDecision(AgentInput decision)
    {
        recentInputs.Enqueue(decision);
        
        if (recentInputs.Count > InputHistorySize)
        {
            recentInputs.Dequeue();
        }
        
        consecutiveDecisions++;
    }
    
    #endregion
}

/// <summary>
/// Performance metrics for agent analysis
/// </summary>
[System.Serializable]
public struct AgentPerformanceMetrics
{
    public int agentId;
    public float totalSimulationTime;
    public int totalSteps;
    public float averageSpeed;
    public float maxSpeed;
    public float totalDistance;
    public float timeOffTrack;
    public int offTrackCount;
    public int checkpointsCompleted;
    public float stepsPerSecond;
    
    public float OffTrackPercentage => totalSimulationTime > 0 ? timeOffTrack / totalSimulationTime : 0f;
    public float AverageStepDistance => totalSteps > 0 ? totalDistance / totalSteps : 0f;
    public float CompletionRate => checkpointsCompleted; // Can be compared against total checkpoints
}

/// <summary>
/// Factory for creating different types of agents
/// </summary>
public static class AgentFactory
{
    /// <summary>
    /// Create a training agent with specific characteristics
    /// </summary>
    public static LightweightAgent CreateTrainingAgent(int agentId, Vector2D startPosition, Vector2D startDirection, 
                                                     MotorcycleConfig config, float explorationRate = 0.1f)
    {
        var agent = new LightweightAgent(agentId, startPosition, startDirection, config);
        agent.decisionMaker.SetExplorationRate(explorationRate);
        return agent;
    }
    
    /// <summary>
    /// Create an agent with deterministic behavior (for testing)
    /// </summary>
    public static LightweightAgent CreateDeterministicAgent(int agentId, Vector2D startPosition, Vector2D startDirection, 
                                                           MotorcycleConfig config, int randomSeed)
    {
        var configWithSeed = config;
        return new LightweightAgent(agentId, startPosition, startDirection, configWithSeed);
    }
}