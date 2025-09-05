using System;
using System.Collections.Generic;

/// <summary>
/// Lightweight agent state structure for multithreaded training
/// Contains all necessary data without Unity dependencies
/// </summary>
[System.Serializable]
public struct ThreadedAgentState
{
    public int agentId;
    public Vector2D position;
    public Vector2D direction; // Unit vector representing facing direction
    public float speed; // Current speed in m/s
    public float turnAngle; // Current turn angle in radians
    public AgentInput lastInput;
    public float timeAlive; // Time since spawn/reset
    public bool isActive;
    
    // Training specific data
    public int currentTargetCheckpoint;
    public int checkpointsCompleted;
    public float lapStartTime;
    public float totalDistance; // Total distance traveled
    public bool hasCompletedLap;
    
    // Performance metrics
    public float averageSpeed;
    public float maxSpeed;
    public float timeOffTrack;
    public int offTrackCount;

    public ThreadedAgentState(int id, Vector2D startPosition, Vector2D startDirection)
    {
        agentId = id;
        position = startPosition;
        direction = startDirection.Normalized;
        speed = 0f;
        turnAngle = 0f;
        lastInput = new AgentInput();
        timeAlive = 0f;
        isActive = true;
        
        currentTargetCheckpoint = 0;
        checkpointsCompleted = 0;
        lapStartTime = 0f;
        totalDistance = 0f;
        hasCompletedLap = false;
        
        averageSpeed = 0f;
        maxSpeed = 0f;
        timeOffTrack = 0f;
        offTrackCount = 0;
    }

    public void Reset(Vector2D newPosition, Vector2D newDirection)
    {
        position = newPosition;
        direction = newDirection.Normalized;
        speed = 0f;
        turnAngle = 0f;
        lastInput = new AgentInput();
        timeAlive = 0f;
        isActive = true;
        
        currentTargetCheckpoint = 0;
        checkpointsCompleted = 0;
        lapStartTime = 0f;
        totalDistance = 0f;
        hasCompletedLap = false;
        
        averageSpeed = 0f;
        maxSpeed = 0f;
        timeOffTrack = 0f;
        offTrackCount = 0;
    }
}

/// <summary>
/// Agent input structure representing throttle and steering decisions
/// </summary>
[System.Serializable]
public struct AgentInput
{
    public int throttle; // -1 (brake), 0 (coast), 1 (accelerate)
    public int steering; // -1 (left), 0 (straight), 1 (right)

    public AgentInput(int throttle, int steering)
    {
        this.throttle = MathUtils.Clamp(throttle, -1, 1);
        this.steering = MathUtils.Clamp(steering, -1, 1);
    }

    public static AgentInput Zero => new AgentInput(0, 0);
    public static AgentInput Accelerate => new AgentInput(1, 0);
    public static AgentInput Brake => new AgentInput(-1, 0);
    public static AgentInput TurnLeft => new AgentInput(0, -1);
    public static AgentInput TurnRight => new AgentInput(0, 1);

    public override string ToString() => $"T:{throttle}, S:{steering}";
}

/// <summary>
/// Configuration structure for motorcycle physics
/// Replaces the Unity-dependent MotorcyclePhysicsConfig
/// </summary>
[System.Serializable]
public struct MotorcycleConfig
{
    // Engine & Power
    public float enginePower; // Watts
    public float maxTractionForce; // Newtons
    public float brakingForce; // Newtons
    
    // Physical Properties
    public float mass; // kg
    public float dragCoefficient;
    public float frontalArea; // m²
    public float rollingResistanceCoefficient;
    
    // Turning
    public float turnRate; // degrees/sec
    public float steeringDecay; // 0-1 range
    public float minSteeringSpeed; // m/s
    public float fullSteeringSpeed; // m/s
    public float steeringIntensity; // affects speed-based steering reduction
    
    // Default configuration matching original MotorcycleAgent settings
    public static MotorcycleConfig Default => new MotorcycleConfig
    {
        enginePower = 150000f,
        maxTractionForce = 7000f,
        brakingForce = 8000f,
        mass = 200f,
        dragCoefficient = 0.6f,
        frontalArea = 0.48f,
        rollingResistanceCoefficient = 0.012f,
        turnRate = 100f,
        steeringDecay = 0.9f,
        minSteeringSpeed = 0.5f,
        fullSteeringSpeed = 5f,
        steeringIntensity = 0.5f
    };
}

/// <summary>
/// Training session data structure
/// </summary>
[System.Serializable]
public struct TrainingSessionThreaded
{
    public int sessionId;
    public int startCheckpoint;
    public int goalCheckpoint;
    public int validateCheckpoint;
    public Vector2D startPosition;
    public Vector2D startDirection;
    public float bestTime;
    public bool isComplete;
    public List<AgentInput> bestInputSequence;
    
    public TrainingSessionThreaded(int id, int start, int goal, int validate, Vector2D startPos, Vector2D startDir)
    {
        sessionId = id;
        startCheckpoint = start;
        goalCheckpoint = goal;
        validateCheckpoint = validate;
        startPosition = startPos;
        startDirection = startDir;
        bestTime = float.MaxValue;
        isComplete = false;
        bestInputSequence = new List<AgentInput>();
    }
}

/// <summary>
/// Training progress data for Unity interface
/// Thread-safe data structure for querying training state
/// </summary>
[System.Serializable]
public struct TrainingProgress
{
    public int currentSession;
    public int totalSessions;
    public int currentIteration;
    public int totalIterations;
    public int activeAgents;
    public float bestTimeThisSession;
    public float averageTimeThisSession;
    public int completedRuns;
    public bool isTraining;
    public double trainingStartTime;
    public double elapsedTrainingTime;
    
    // Performance metrics
    public float iterationsPerSecond;
    public float agentStepsPerSecond;
    public int totalAgentSteps;
}

/// <summary>
/// Agent visualization data for Unity rendering
/// Lightweight structure for real-time visualization updates
/// </summary>
[System.Serializable]
public struct AgentVisualizationData
{
    public int agentId;
    public Vector2D position;
    public Vector2D direction;
    public float speed;
    public bool isActive;
    public bool isOffTrack;
    public int currentCheckpoint;
    public float completionPercentage; // 0-1 for current session progress

    public AgentVisualizationData(ThreadedAgentState state, bool offTrack, float completion)
    {
        agentId = state.agentId;
        position = state.position;
        direction = state.direction;
        speed = state.speed;
        isActive = state.isActive;
        isOffTrack = offTrack;
        currentCheckpoint = state.currentTargetCheckpoint;
        completionPercentage = completion;
    }
}