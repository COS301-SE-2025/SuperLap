using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class AgentState
{
    public Vector3 position;
    public Vector3 forward;
    public float speed;
    public float turnAngle;
    public List<(int, int)> inputSequence;
    public float timeToGoal; // Time from start to validate checkpoint (complete run time)
    public bool isValid;
    
    public AgentState()
    {
        inputSequence = new List<(int, int)>();
        isValid = false;
    }
    
    public AgentState(Vector3 pos, Vector3 fwd, float spd, float turn)
    {
        position = pos;
        forward = fwd;
        speed = spd;
        turnAngle = turn;
        inputSequence = new List<(int, int)>();
        isValid = false;
    }
}

[System.Serializable]
public class TrainingSession
{
    public int startCheckpoint;
    public int goalCheckpoint; 
    public int validateCheckpoint;
    public AgentState bestState;
    public float bestTime;
    public bool isComplete;
    
    public TrainingSession(int start, int goal, int validate)
    {
        startCheckpoint = start;
        goalCheckpoint = goal;
        validateCheckpoint = validate;
        bestState = new AgentState();
        bestTime = float.MaxValue;
        isComplete = false;
    }
}

public class Trainer : MonoBehaviour
{
    [Header("Training Parameters")]
    [SerializeField] private int agentCount = 4;
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int decisionInterval = 1;
    [SerializeField] private int iterations = 100;
    
    [Header("Training State - Debug")]
    [SerializeField] private int currentIteration = 0;
    [SerializeField] private int currentSessionIndex = 0;
    [SerializeField] private int activeAgents = 0;
    [SerializeField] private bool isTraining = false;
    
    // Training data structures
    private List<TrainingSession> trainingSessions;
    
    // Public accessors for camera controller
    public List<TrainingSession> TrainingSessions => trainingSessions;
    public int CurrentSessionIndex => currentSessionIndex;
    public bool IsTraining => isTraining;
    
    // Public accessors for UI controller
    public int AgentCount => agentCount;
    public int Iterations => iterations;
    public int CurrentIteration => currentIteration;
    public int ActiveAgents => activeAgents;
    private List<MotorcycleAgent> agents;
    private List<AgentState> agentStates;
    private List<AgentState> agentGoalStates; // States captured at goal checkpoint for next session
    private List<int> agentCheckpointTargets; // Which checkpoint each agent is heading for
    private List<float> agentStartTimes;
    private Queue<int> availableAgentSlots;
    private int currentStep = 0;
    private CheckpointManager checkpointManager;
    
    // Checkpoint state
    private List<int> allCheckpoints;
    private Vector3 currentStartPos;
    private Vector3 currentStartDir;
    private AgentState currentTrainingState;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Get checkpoint data
        checkpointManager = FindAnyObjectByType<CheckpointManager>();
        if (checkpointManager == null)
        {
            Debug.LogError("CheckpointManager not found! Cannot start training.");
            return;
        }
        
        // Get all checkpoint IDs
        allCheckpoints = new List<int>();
        for (int i = 0; i < checkpointManager.GetTotalCheckpoints(); i++)
        {
            allCheckpoints.Add(i);
        }
        
        if (allCheckpoints.Count < 3)
        {
            Debug.LogError("Need at least 3 checkpoints for training!");
            return;
        }
        
        // Create training sessions for every 3 consecutive checkpoints
        trainingSessions = new List<TrainingSession>();
        for (int i = 0; i < allCheckpoints.Count - 2; i++)
        {
            trainingSessions.Add(new TrainingSession(allCheckpoints[i], allCheckpoints[i + 1], allCheckpoints[i + 2]));
        }
        
        // Initialize agent management
        agents = new List<MotorcycleAgent>();
        agentStates = new List<AgentState>();
        agentGoalStates = new List<AgentState>();
        agentCheckpointTargets = new List<int>();
        agentStartTimes = new List<float>();
        availableAgentSlots = new Queue<int>();
        
        for (int i = 0; i < agentCount; i++)
        {
            agents.Add(null);
            agentStates.Add(null);
            agentGoalStates.Add(null);
            agentCheckpointTargets.Add(-1);
            agentStartTimes.Add(0f);
            availableAgentSlots.Enqueue(i);
        }
        
        // Set initial training state (start of first checkpoint)
        currentTrainingState = new AgentState(
            TrackMaster.GetTrainingSpawnPosition(0, TrackMaster.GetCurrentRaceline()),
            TrackMaster.GetTrainingSpawnDirection(0, TrackMaster.GetCurrentRaceline()),
            0f,
            0f
        );
        
        currentSessionIndex = 0;
        currentIteration = 0;
        activeAgents = 0;
        
        SetupCheckpointVisualization();
        
        Debug.Log($"Training initialized with {trainingSessions.Count} sessions and {agentCount} agents");
    }

    void SetupCheckpointVisualization()
    {
        if (currentSessionIndex >= trainingSessions.Count) return;
        
        var session = trainingSessions[currentSessionIndex];
        
        // Set checkpoint materials: start=material[0], goal=material[1], validate=material[2]
        checkpointManager.SetTargetCheckpoint(session.startCheckpoint);
        checkpointManager.SetMaxVisibleCheckpoints(3);
        
        Debug.Log($"Session {currentSessionIndex}: Start={session.startCheckpoint}, Goal={session.goalCheckpoint}, Validate={session.validateCheckpoint}");
    }

    void FixedUpdate()
    {
        if (!isTraining) return;
        
        // Spawn new agents if slots are available and iterations remain
        if (currentIteration < iterations && availableAgentSlots.Count > 0)
        {
            SpawnNewAgent();
        }
        
        // Update existing agents
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i] != null)
            {
                UpdateAgent(i);
            }
        }
        
        currentStep++;
    }

    void SpawnNewAgent()
    {
        if (availableAgentSlots.Count == 0) return;
        
        int slotIndex = availableAgentSlots.Dequeue();
        
        // Create agent at current training state position
        GameObject obj = Instantiate(agentPrefab);
        obj.transform.position = currentTrainingState.position;
        obj.transform.rotation = Quaternion.LookRotation(currentTrainingState.forward);
        
        MotorcycleAgent agent = obj.GetComponent<MotorcycleAgent>();
        agents[slotIndex] = agent;
        
        // Set the agent's initial speed and turn angle to match the training state
        agent.SetInitialState(currentTrainingState.speed, currentTrainingState.turnAngle);
        
        // Initialize agent state
        agentStates[slotIndex] = new AgentState(currentTrainingState.position, currentTrainingState.forward, 
                                              currentTrainingState.speed, currentTrainingState.turnAngle);
        agentCheckpointTargets[slotIndex] = trainingSessions[currentSessionIndex].validateCheckpoint; // Target the final checkpoint
        agentStartTimes[slotIndex] = Time.time;
        
        activeAgents++;
        currentIteration++;
        
        Debug.Log($"Spawned agent {slotIndex} for iteration {currentIteration}/{iterations} " +
                 $"at position {currentTrainingState.position} with speed {currentTrainingState.speed:F2} m/s " +
                 $"and turn angle {currentTrainingState.turnAngle:F2}° - targeting validate checkpoint {trainingSessions[currentSessionIndex].validateCheckpoint}");
    }

    void UpdateAgent(int agentIndex)
    {
        var agent = agents[agentIndex];
        var state = agentStates[agentIndex];
        
        if (agent == null || state == null) return;
        
        // Update agent
        agent.Step();
        
        // Record inputs on decision intervals
        if ((currentStep + agentIndex) % decisionInterval == 0)
        {
            var action = agent.Decide();
            agent.SetInput(action.Item1, action.Item2);
            state.inputSequence.Add(action);
        }
        
        // Check if agent went off track
        if (IsAgentOffTrack(agent))
        {
            RecycleAgent(agentIndex, false, "Went off track");
            return;
        }
        
        // Update agent state
        state.position = agent.transform.position;
        state.forward = agent.transform.forward;
        state.speed = agent.GetCurrentSpeed();
        state.turnAngle = agent.GetCurrentTurnAngle();
    }

    public void OnCheckpointTriggered(int checkpointId, GameObject triggerer)
    {
        if (!isTraining) return;
        
        // Find which agent triggered this checkpoint
        int agentIndex = -1;
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i] != null && agents[i].gameObject == triggerer)
            {
                agentIndex = i;
                break;
            }
        }
        
        if (agentIndex == -1) return;
        
        var session = trainingSessions[currentSessionIndex];
        
        if (checkpointId == session.goalCheckpoint)
        {
            // Agent reached goal checkpoint - capture state for next session
            agentGoalStates[agentIndex] = new AgentState(
                agents[agentIndex].transform.position,
                agents[agentIndex].transform.forward,
                agents[agentIndex].GetCurrentSpeed(),
                agents[agentIndex].GetCurrentTurnAngle()
            );
            agentGoalStates[agentIndex].inputSequence = new List<(int, int)>(agentStates[agentIndex].inputSequence);
            
            float timeToGoal = Time.time - agentStartTimes[agentIndex];
            Debug.Log($"Agent {agentIndex} reached goal checkpoint in {timeToGoal:F2}s at speed {agentGoalStates[agentIndex].speed:F2} m/s, continuing to validate");
        }
        else if (checkpointId == session.validateCheckpoint)
        {
            // Agent completed the full run from start to validate checkpoint
            float totalTime = Time.time - agentStartTimes[agentIndex];
            var state = agentStates[agentIndex];
            state.timeToGoal = totalTime;
            state.isValid = true;
            
            // Check if this is the best complete time so far
            if (totalTime < session.bestTime)
            {
                session.bestTime = totalTime;
                
                // Use the goal state (if captured) for the next session's starting point
                if (agentGoalStates[agentIndex] != null)
                {
                    session.bestState = new AgentState(
                        agentGoalStates[agentIndex].position,
                        agentGoalStates[agentIndex].forward,
                        agentGoalStates[agentIndex].speed,
                        agentGoalStates[agentIndex].turnAngle
                    );
                    session.bestState.inputSequence = new List<(int, int)>(agentGoalStates[agentIndex].inputSequence);
                }
                else
                {
                    // Fallback if goal state wasn't captured (shouldn't happen in normal flow)
                    session.bestState = new AgentState(state.position, state.forward, state.speed, state.turnAngle);
                    session.bestState.inputSequence = new List<(int, int)>(state.inputSequence);
                }
                
                session.bestState.timeToGoal = totalTime;
                session.bestState.isValid = true;
                
                Debug.Log($"NEW BEST COMPLETE TIME for session {currentSessionIndex}: {totalTime:F2}s");
            }
            
            RecycleAgent(agentIndex, true, $"Completed full run in {totalTime:F2}s");
        }
    }

    void RecycleAgent(int agentIndex, bool wasSuccessful, string reason)
    {
        if (agents[agentIndex] != null)
        {
            Destroy(agents[agentIndex].gameObject);
            agents[agentIndex] = null;
        }
        
        agentStates[agentIndex] = null;
        agentGoalStates[agentIndex] = null;
        agentCheckpointTargets[agentIndex] = -1;
        agentStartTimes[agentIndex] = 0f;
        availableAgentSlots.Enqueue(agentIndex);
        activeAgents--;
        
        Debug.Log($"Recycled agent {agentIndex}: {reason}");
        
        // Check if we should move to next session
        if (currentIteration >= iterations && activeAgents == 0)
        {
            CompleteCurrentSession();
        }
    }

    void CompleteCurrentSession()
    {
        var session = trainingSessions[currentSessionIndex];
        session.isComplete = true;
        
        if (session.bestState.isValid)
        {
            // Update training state for next session using the best run's goal crossing state
            currentTrainingState = new AgentState(
                session.bestState.position,
                session.bestState.forward,
                session.bestState.speed,
                session.bestState.turnAngle
            );
            currentTrainingState.inputSequence = new List<(int, int)>();
            currentTrainingState.isValid = true;
            
            Debug.Log($"Session {currentSessionIndex} completed! Best complete time (start->validate): {session.bestTime:F2}s. " +
                     $"Next session starts at goal checkpoint with speed: {currentTrainingState.speed:F2} m/s");
        }
        else
        {
            Debug.LogWarning($"Session {currentSessionIndex} completed but no valid runs found! " +
                           $"Keeping previous training state (speed: {currentTrainingState.speed:F2} m/s)");
        }
        
        // Move to next session
        currentSessionIndex++;
        currentIteration = 0;
        
        if (currentSessionIndex >= trainingSessions.Count)
        {
            CompleteTraining();
        }
        else
        {
            SetupCheckpointVisualization();
        }
    }

    void CompleteTraining()
    {
        isTraining = false;
        Debug.Log("Training completed for all sessions!");
        
        // Output summary
        for (int i = 0; i < trainingSessions.Count; i++)
        {
            var session = trainingSessions[i];
            Debug.Log($"Session {i}: Start={session.startCheckpoint} -> Goal={session.goalCheckpoint} -> Validate={session.validateCheckpoint} " +
                     $"Best complete time: {session.bestTime:F2}s Valid: {session.bestState.isValid}");
        }
    }

    bool IsAgentOffTrack(MotorcycleAgent agent)
    {
        return agent.IsOffTrack();
    }

    #region Public Methods for UI Control
    
    public void SetAgentCount(int count)
    {
        if (!isTraining && count > 0)
        {
            agentCount = Mathf.Clamp(count, 1, 10);
            Debug.Log($"Agent count set to: {agentCount}");
        }
    }
    
    public void SetIterations(int count)
    {
        if (!isTraining && count > 0)
        {
            iterations = count;
            Debug.Log($"Iterations set to: {iterations}");
        }
    }
    
    public void StartTraining()
    {
        if (!isTraining)
        {
            isTraining = true;
            Debug.Log("Training started via UI");
        }
    }
    
    public void StopTraining()
    {
        if (isTraining)
        {
            isTraining = false;
            Debug.Log("Training stopped via UI");
        }
    }
    
    public void ResetTraining()
    {
        StopTraining();
        Clear();
        Initialize();
        Debug.Log("Training reset via UI");
    }
    
    #endregion

    void Clear()
    {
        foreach (var agent in agents)
        {
            if (agent != null)
            {
                Destroy(agent.gameObject);
            }
        }
        
        agents.Clear();
        agentStates.Clear();
        agentGoalStates.Clear();
        agentCheckpointTargets.Clear();
        agentStartTimes.Clear();
        availableAgentSlots.Clear();
        
        activeAgents = 0;
        currentIteration = 0;
        currentSessionIndex = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Clear();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Initialize();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            isTraining = !isTraining;
            if (isTraining)
            {
                Debug.Log("Training started");
            }
            else
            {
                Debug.Log("Training stopped");
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    agent.Step();
                }
            }
        }
    }
}
