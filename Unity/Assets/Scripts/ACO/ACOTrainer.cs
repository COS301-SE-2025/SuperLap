using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading;

[System.Serializable]
public class ACOAgentState
{
    public System.Numerics.Vector2 position;
    public System.Numerics.Vector2 forward;
    public float speed;
    public float turnAngle;
    public List<(int, int)> inputSequence;
    public float timeToGoal; // Time from start to validate checkpoint (complete run time)
    public bool isValid;
    
    public ACOAgentState()
    {
        inputSequence = new List<(int, int)>();
        isValid = false;
    }
    
    public ACOAgentState(System.Numerics.Vector2 pos, System.Numerics.Vector2 fwd, float spd, float turn)
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
public class ACOTrainingSession
{
    public int startCheckpoint;
    public int goalCheckpoint; 
    public int validateCheckpoint;
    public ACOAgentState bestState;
    public float bestTime;
    public bool isComplete;
    
    public ACOTrainingSession(int start, int goal, int validate)
    {
        startCheckpoint = start;
        goalCheckpoint = goal;
        validateCheckpoint = validate;
        bestState = new ACOAgentState();
        bestTime = float.MaxValue;
        isComplete = false;
    }
}

public class ACOTrainer : MonoBehaviour
{
    [Header("Training Parameters")]
    [SerializeField] private int agentCount = 4;
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int decisionInterval = 1;
    [SerializeField] private int iterations = 100;
    
    [Header("Threading Configuration")]
    [SerializeField] private bool enableMultithreading = true;
    [SerializeField] private int threadCount = 2;
    [SerializeField] private int agentsPerThread = 2;
    
    [Header("Training State - Debug")]
    [SerializeField] private int currentIteration = 0; // Successful iterations only
    [SerializeField] private int totalAttempts = 0; // Total agent spawns (including failures)
    [SerializeField] private int currentSessionIndex = 0;
    [SerializeField] private int activeAgents = 0;
    [SerializeField] private bool isTraining = false;
    
    // Training data structures
    private List<ACOTrainingSession> trainingSessions;
    
    // Public accessors for camera controller
    public List<ACOTrainingSession> TrainingSessions => trainingSessions;
    public int CurrentSessionIndex => currentSessionIndex;
    public bool IsTraining => isTraining;
    
    // Public accessors for UI controller
    public int AgentCount => agentCount;
    public int Iterations => iterations;
    public int CurrentIteration => currentIteration;
    public int TotalAttempts => totalAttempts;
    public int ActiveAgents => activeAgents;
    private Dictionary<int, ACOAgentComponent> agentComponents;
    private List<ACOAgent> agents;
    private List<ACOAgentState> agentStates;
    private List<ACOAgentState> agentGoalStates; // States captured at goal checkpoint for next session
    private List<int> agentCheckpointTargets; // Which checkpoint each agent is heading for
    private List<float> agentStartTimes;
    private Queue<int> availableAgentSlots;
    private int currentStep = 0;
    
    // Threading infrastructure
    private List<ACOWorkerThread> workerThreads;
    private ConcurrentQueue<AgentEvent> agentEventQueue;
    private int nextAgentId = 0;
    
    // New distance-based checkpoint system
    [Header("Distance-Based Checkpoint System")]
    [SerializeField] private float checkpointTriggerDistance = 5f; // Distance threshold to trigger checkpoint
    [SerializeField] private bool useDistanceBasedCheckpoints = true;
    [SerializeField] private int totalCheckpoints = 10; // Number of checkpoints to create
    
    // Checkpoint tracking data
    private List<System.Numerics.Vector3> checkpointPositions; // Positions of checkpoints on raceline
    private List<float> checkpointDistances; // Distance along raceline for each checkpoint
    private Dictionary<int, int> agentCurrentCheckpoints; // Current target checkpoint for each agent
    private Dictionary<int, float> agentCheckpointProgress; // Progress toward next checkpoint for each agent
    private Dictionary<int, bool> agentCheckpointTriggered; // Track if checkpoint was just triggered
    private float totalRacelineDistance;
    
    // Checkpoint state
    private List<int> allCheckpoints;
    private Vector3 currentStartPos;
    private Vector3 currentStartDir;
    private ACOAgentState currentTrainingState;

    void Start()
    {

    }

    void Initialize()
    {
        // Create a simple checkpoint list (can be configured or loaded from elsewhere)
        allCheckpoints = new List<int>();
        for (int i = 0; i < totalCheckpoints; i++)
        {
            allCheckpoints.Add(i);
        }
        
        if (allCheckpoints.Count < 3)
        {
            Debug.LogError("Need at least 3 checkpoints for training!");
            return;
        }
        
        // Create training sessions for every 3 consecutive checkpoints
        trainingSessions = new List<ACOTrainingSession>();
        for (int i = 0; i < allCheckpoints.Count - 2; i++)
        {
            trainingSessions.Add(new ACOTrainingSession(allCheckpoints[i], allCheckpoints[i + 1], allCheckpoints[i + 2]));
        }
        
        // Initialize agent management
        agents = new List<ACOAgent>();
        agentStates = new List<ACOAgentState>();
        agentGoalStates = new List<ACOAgentState>();
        agentCheckpointTargets = new List<int>();
        agentStartTimes = new List<float>();
        availableAgentSlots = new Queue<int>();
        agentComponents = new Dictionary<int, ACOAgentComponent>();
        
        // Initialize threading infrastructure
        if (enableMultithreading)
        {
            InitializeThreading();
        }
        
        // Initialize distance-based checkpoint tracking
        if (useDistanceBasedCheckpoints)
        {
            InitializeDistanceBasedCheckpoints();
        }
        
        agentCurrentCheckpoints = new Dictionary<int, int>();
        agentCheckpointProgress = new Dictionary<int, float>();
        agentCheckpointTriggered = new Dictionary<int, bool>();
        
        for (int i = 0; i < agentCount; i++)
        {
            agents.Add(null);
            agentStates.Add(null);
            agentGoalStates.Add(null);
            agentCheckpointTargets.Add(-1);
            agentStartTimes.Add(0f);
            availableAgentSlots.Enqueue(i);
        }
        
        Vector3 startPos = ACOTrackMaster.GetTrainingSpawnPosition(0, ACOTrackMaster.GetCurrentRaceline());
        Vector3 startDir = ACOTrackMaster.GetTrainingSpawnDirection(0, ACOTrackMaster.GetCurrentRaceline());

        // Set initial training state (start of first checkpoint)
        currentTrainingState = new ACOAgentState(
            new System.Numerics.Vector2(startPos.x, startPos.z),
            new System.Numerics.Vector2(startDir.x, startDir.z),
            0f,
            0f
        );
        
        currentSessionIndex = 0;
        currentIteration = 0;
        totalAttempts = 0;
        activeAgents = 0;
        
        SetupCheckpointVisualization();
        
        Debug.Log($"Training initialized with {trainingSessions.Count} sessions and {agentCount} agents");
    }

    void SetupCheckpointVisualization()
    {
        if (currentSessionIndex >= trainingSessions.Count) return;
        
        var session = trainingSessions[currentSessionIndex];
        
        // Log checkpoint configuration for this session
        Debug.Log($"Session {currentSessionIndex}: Start={session.startCheckpoint}, Goal={session.goalCheckpoint}, Validate={session.validateCheckpoint}");
        
        // If using distance-based checkpoints, no additional setup needed
        // The distance-based system handles its own visualization through OnDrawGizmos
    }

    void FixedUpdate()
    {
        if (!isTraining) return;
        
        if (enableMultithreading)
        {
            // Process events from worker threads
            ProcessAgentEvents();
            
            // Update visual representations
            UpdateAgentVisuals();
            
            // Spawn new agents in worker threads if needed
            if (currentIteration < iterations)
            {
                SpawnAgentsInThreads();
            }
        }
        else
        {
            // Original single-threaded logic
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
        }
        
        currentStep++;
    }

    void SpawnNewAgent()
    {
        if (availableAgentSlots.Count == 0) return;
        
        int slotIndex = availableAgentSlots.Dequeue();
        
        // Create agent at current training state position
        GameObject obj = Instantiate(agentPrefab);
        obj.transform.position = new Vector3(currentTrainingState.position.X, 0, currentTrainingState.position.Y);
        obj.transform.rotation = Quaternion.LookRotation(new Vector3(currentTrainingState.forward.X, 0, currentTrainingState.forward.Y));

        // Calculate bearing from forward vector
        float bearing = CalculateBearing(currentTrainingState.forward);
        
        // Create new ACOAgent instance
        ACOAgent agent = new ACOAgent(ACOTrackMaster.CreatePolygonTrackCopy(), currentTrainingState.position, bearing, ACOTrackMaster.CreateRacelineCopy());
        agents[slotIndex] = agent;
        
        // Get and link the component
        ACOAgentComponent agentComponent = obj.GetComponent<ACOAgentComponent>();
        if (agentComponent == null)
        {
            // If the prefab doesn't have ACOAgentComponent, add it
            agentComponent = obj.AddComponent<ACOAgentComponent>();
            Debug.LogWarning("ACOAgentComponent was missing from agentPrefab, added it automatically.");
        }
        agentComponents[agent.ID] = agentComponent;
                
        // Set the agent's initial speed and turn angle to match the training state
        agent.SetInitialState(currentTrainingState.speed, currentTrainingState.turnAngle);

        // Initialize agent state
        agentStates[slotIndex] = new ACOAgentState(currentTrainingState.position, currentTrainingState.forward, 
                                              currentTrainingState.speed, currentTrainingState.turnAngle);
        agentCheckpointTargets[slotIndex] = trainingSessions[currentSessionIndex].validateCheckpoint; // Target the final checkpoint
        agentStartTimes[slotIndex] = Time.time;
        
        activeAgents++;
        totalAttempts++;
    }

    void UpdateAgent(int agentIndex)
    {
        var agent = agents[agentIndex];
        var state = agentStates[agentIndex];
        
        if (agent == null || state == null) return;
        
        // Update agent
        agent.Step();
        
        // Sync component transform with agent position
        if (agentComponents.ContainsKey(agent.ID))
        {
            var component = agentComponents[agent.ID];
            if (component != null)
            {
                component.transform.position = new Vector3(agent.Position.X, component.transform.position.y, agent.Position.Y);
                
                // Update rotation based on agent's forward direction
                Vector3 forward3D = new Vector3(agent.Forward.X, 0, agent.Forward.Y);
                if (forward3D.magnitude > 0.001f)
                {
                    component.transform.rotation = Quaternion.LookRotation(forward3D);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Agent ID {agent.ID} not found in agentComponents dictionary!");
        }
        
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
        
        // Check distance-based checkpoints
        if (useDistanceBasedCheckpoints)
        {
            CheckDistanceBasedCheckpoints(agentIndex);
        }
        
        // Update agent state
        state.position = agent.Position;
        state.forward = agent.Forward;
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
            // TODO: Fix this when implementing new checkpoint system
            // Need to map from component GameObject to agent via ID
            // if (agents[i] != null && agents[i].gameObject == triggerer)
            // {
            //     agentIndex = i;
            //     break;
            // }
        }
        
        if (agentIndex == -1) return;
        
        var session = trainingSessions[currentSessionIndex];
        
        if (checkpointId == session.goalCheckpoint)
        {
            // Agent reached goal checkpoint - capture state for next session
            agentGoalStates[agentIndex] = new ACOAgentState(
                agents[agentIndex].Position,
                agents[agentIndex].Forward,
                agents[agentIndex].GetCurrentSpeed(),
                agents[agentIndex].GetCurrentTurnAngle()
            );
            agentGoalStates[agentIndex].inputSequence = new List<(int, int)>(agentStates[agentIndex].inputSequence);
            
            float timeToGoal = Time.time - agentStartTimes[agentIndex];
            //TODO
            // Debug.Log($"Agent {agentIndex} reached goal checkpoint in {timeToGoal:F2}s at speed {agentGoalStates[agentIndex].speed:F2} m/s, continuing to validate");
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
                    session.bestState = new ACOAgentState(
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
                    session.bestState = new ACOAgentState(state.position, state.forward, state.speed, state.turnAngle);
                    session.bestState.inputSequence = new List<(int, int)>(state.inputSequence);
                }

                session.bestState.timeToGoal = totalTime;
                session.bestState.isValid = true;

                // TODO
                // Debug.Log($"NEW BEST COMPLETE TIME for session {currentSessionIndex}: {totalTime:F2}s");
            }
            
            RecycleAgent(agentIndex, true, $"Completed full run in {totalTime:F2}s");
        }
    }

    void RecycleAgent(int agentIndex, bool wasSuccessful, string reason)
    {
        if (agents[agentIndex] != null)
        {
            int agentId = agents[agentIndex].ID;
            
            // Clean up distance-based checkpoint tracking
            if (useDistanceBasedCheckpoints)
            {
                agentCurrentCheckpoints.Remove(agentId);
                agentCheckpointProgress.Remove(agentId);
                agentCheckpointTriggered.Remove(agentId);
            }
            
            // Destroy the component GameObject and remove from dictionary
            if (agentComponents.ContainsKey(agentId))
            {
                Destroy(agentComponents[agentId].gameObject);
                agentComponents.Remove(agentId);
            }
            agents[agentIndex] = null;
        }
        
        // Only count successful runs as iterations
        if (wasSuccessful)
        {
            currentIteration++;
            // TODO
            // Debug.Log($"Recycled agent {agentIndex}: {reason} - SUCCESSFUL iteration {currentIteration}/{iterations}");
        }
        else
        {
            // TODO
            // Debug.Log($"Recycled agent {agentIndex}: {reason} - FAILED attempt (successful iterations: {currentIteration}/{iterations})");
        }
        
        agentStates[agentIndex] = null;
        agentGoalStates[agentIndex] = null;
        agentCheckpointTargets[agentIndex] = -1;
        agentStartTimes[agentIndex] = 0f;
        availableAgentSlots.Enqueue(agentIndex);
        activeAgents--;
        
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
            currentTrainingState = new ACOAgentState(
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
        totalAttempts = 0;
        
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
        
        // Output summary with efficiency metrics
        int totalSuccessfulRuns = 0;
        
        for (int i = 0; i < trainingSessions.Count; i++)
        {
            var session = trainingSessions[i];
            totalSuccessfulRuns += (session.isComplete && session.bestState.isValid) ? iterations : 0;
            Debug.Log($"Session {i}: Start={session.startCheckpoint} -> Goal={session.goalCheckpoint} -> Validate={session.validateCheckpoint} " +
                     $"Best complete time: {session.bestTime:F2}s Valid: {session.bestState.isValid}");
        }
        
        Debug.Log($"Training Summary: {totalSuccessfulRuns} successful runs completed across all sessions");
        
        // Stop all worker threads
        if (enableMultithreading && workerThreads != null)
        {
            foreach (var worker in workerThreads)
            {
                worker.StopThread();
            }
        }
    }
    
    void InitializeThreading()
    {
        agentEventQueue = new ConcurrentQueue<AgentEvent>();
        workerThreads = new List<ACOWorkerThread>();
        
        // Create worker threads with their own buffers and track copies
        for (int i = 0; i < threadCount; i++)
        {
            var buffer = new WorkerThreadBuffer(agentsPerThread);
            
            // Create unique track and raceline copies for this thread to eliminate memory contention
            var threadTrack = ACOTrackMaster.CreatePolygonTrackCopy();
            var threadRaceline = ACOTrackMaster.CreateRacelineCopy();
            
            var worker = new ACOWorkerThread(
                buffer,
                agentEventQueue,
                threadTrack,  // Use thread-specific track copy
                threadRaceline, // Use thread-specific raceline copy
                decisionInterval,
                i  // Pass worker ID for logging
            );
            workerThreads.Add(worker);
            worker.StartThread();
        }
        
        Debug.Log($"Initialized {threadCount} worker threads for multithreaded training");
    }
    
    void ProcessAgentEvents()
    {
        while (agentEventQueue.TryDequeue(out AgentEvent agentEvent))
        {
            switch (agentEvent.EventType)
            {
                case AgentEventType.CheckpointTriggered:
                    HandleCheckpointTriggered(agentEvent.AgentId, agentEvent.CheckpointId);
                    break;
                    
                case AgentEventType.AgentCompleted:
                    HandleAgentCompleted(agentEvent);
                    break;
                    
                case AgentEventType.AgentFailed:
                    HandleAgentFailed(agentEvent.AgentId, agentEvent.Reason);
                    break;
            }
        }
    }
    
    void UpdateAgentVisuals()
    {
        if (workerThreads == null) return;
        
        // Read from each worker thread's buffer
        foreach (var worker in workerThreads)
        {
            var buffer = worker.GetBuffer();
            if (buffer == null) continue;
            
            var readBuffer = buffer.GetReadBuffer();
            if (readBuffer == null) continue; // No new data
            
            // Update visuals for all agents in this buffer
            for (int i = 0; i < readBuffer.ActiveCount; i++)
            {
                if (readBuffer.GetAgentState(i, out int agentId, out var position, out var forward,
                                           out float speed, out float turnAngle, out bool active, out bool offTrack))
                {
                    if (!active) continue;
                    
                    // Update the visual component
                    if (agentComponents.ContainsKey(agentId))
                    {
                        var component = agentComponents[agentId];
                        if (component != null)
                        {
                            component.transform.position = new Vector3(position.X, component.transform.position.y, position.Y);
                            
                            Vector3 forward3D = new Vector3(forward.X, 0, forward.Y);
                            if (forward3D.magnitude > 0.001f)
                            {
                                component.transform.rotation = Quaternion.LookRotation(forward3D);
                            }
                        }
                    }
                }
            }
        }
    }
    
    void SpawnAgentsInThreads()
    {
        // Calculate how many total agents we should have running
        int totalActiveAgents = 0;
        if (workerThreads != null)
        {
            foreach (var worker in workerThreads)
            {
                totalActiveAgents += worker.GetActiveAgentCount();
            }
        }
        
        // Spawn new agents if we need more
        int desiredAgents;
        if (enableMultithreading)
        {
            // In multithreading mode, use thread configuration
            desiredAgents = threadCount * agentsPerThread;
        }
        else
        {
            // In single-threaded mode, use agentCount
            desiredAgents = agentCount;
        }
        if (totalActiveAgents < desiredAgents)
        {
            int agentsToSpawn = Mathf.Min(desiredAgents - totalActiveAgents, enableMultithreading ? 50 : 1); // Spawn up to 50 agents per frame in multithreading mode
            int agentsSpawned = 0;
            
            while (agentsSpawned < agentsToSpawn)
            {
                bool spawned = false;
                foreach (var worker in workerThreads)
                {
                    if (worker.GetActiveAgentCount() < agentsPerThread)
                    {
                        SpawnAgentInWorker(worker);
                        totalAttempts++;
                        activeAgents++;
                        agentsSpawned++;
                        spawned = true;
                        
                        if (agentsSpawned >= agentsToSpawn) break;
                    }
                }
                
                // If no workers have space, break to avoid infinite loop
                if (!spawned) break;
            }
        }
    }
    
    void SpawnAgentInWorker(ACOWorkerThread worker)
    {
        // Convert checkpointPositions to Unity Vector3 for worker thread
        List<Vector3> unityCheckpoints = new List<Vector3>();
        if (checkpointPositions != null)
        {
            foreach (var pos in checkpointPositions)
            {
                unityCheckpoints.Add(new Vector3(pos.X, pos.Y, pos.Z));
            }
        }
        
        // Update worker with current training data
        worker.SetTrainingData(currentTrainingState, trainingSessions[currentSessionIndex], unityCheckpoints, checkpointTriggerDistance);
        
        // Spawn agent with unique ID
        int agentId = nextAgentId++;
        worker.SpawnAgent(agentId);
        
        // Create visual representation
        GameObject obj = Instantiate(agentPrefab);
        obj.transform.position = new Vector3(currentTrainingState.position.X, 0, currentTrainingState.position.Y);
        obj.transform.rotation = Quaternion.LookRotation(new Vector3(currentTrainingState.forward.X, 0, currentTrainingState.forward.Y));
        
        ACOAgentComponent agentComponent = obj.GetComponent<ACOAgentComponent>();
        if (agentComponent == null)
        {
            agentComponent = obj.AddComponent<ACOAgentComponent>();
        }
        agentComponents[agentId] = agentComponent;
        
        // Debug.Log($"Spawned agent {agentId} in worker thread (attempt {totalAttempts}, iteration {currentIteration}/{iterations})");
    }
    
    void HandleCheckpointTriggered(int agentId, int checkpointId)
    {
        var session = trainingSessions[currentSessionIndex];

        if (checkpointId == session.goalCheckpoint)
        {
            // TODO
            // Debug.Log($"Agent {agentId} reached goal checkpoint");
            // Goal checkpoint logic handled in worker thread
        }
    }
    
    void HandleAgentCompleted(AgentEvent completionEvent)
    {
        currentIteration++;
        activeAgents--;
        
        var session = trainingSessions[currentSessionIndex];
        
        // Check if this is the best time
        if (completionEvent.CompletionTime < session.bestTime)
        {
            session.bestTime = completionEvent.CompletionTime;
            
            // Update best state (simplified for threading)
            session.bestState = new ACOAgentState(
                new System.Numerics.Vector2(0, 0), // Will be updated properly later
                new System.Numerics.Vector2(1, 0),
                0f, 0f
            );
            session.bestState.timeToGoal = completionEvent.CompletionTime;
            session.bestState.isValid = true;
            
            // TODO
            // Debug.Log($"NEW BEST TIME for session {currentSessionIndex}: {completionEvent.CompletionTime:F2}s");
        }
        
        // Clean up visual component
        if (agentComponents.ContainsKey(completionEvent.AgentId))
        {
            Destroy(agentComponents[completionEvent.AgentId].gameObject);
            agentComponents.Remove(completionEvent.AgentId);
        }
        
        // Remove from shared states (handled by worker thread buffer cleanup)
        
        Debug.Log($"Agent {completionEvent.AgentId} completed in {completionEvent.CompletionTime:F2}s - iteration {currentIteration}/{iterations}");
        
        // Check if session is complete
        if (currentIteration >= iterations && activeAgents == 0)
        {
            CompleteCurrentSession();
        }
    }
    
    void HandleAgentFailed(int agentId, string reason)
    {
        activeAgents--;
        
        // Clean up visual component
        if (agentComponents.ContainsKey(agentId))
        {
            Destroy(agentComponents[agentId].gameObject);
            agentComponents.Remove(agentId);
        }
        
        // Remove from shared states (handled by worker thread buffer cleanup)
        
        Debug.Log($"Agent {agentId} failed: {reason}");
    }

    bool IsAgentOffTrack(ACOAgent agent)
    {
        return agent.IsOffTrack();
    }

    float CalculateBearing(System.Numerics.Vector2 forward)
    {
        // This should be the inverse of ACOAgent.Forward calculation:
        // ACOAgent: rad = (bearing - 90) * π/180; forward = (cos(rad), sin(rad))
        // Inverse: bearing = atan2(forward.Y, forward.X) * 180/π + 90
        float angle = (float)(Math.Atan2(forward.Y, forward.X) * 180.0 / Math.PI);
        return angle + 90.0f;
    }

    void InitializeDistanceBasedCheckpoints()
    {
        checkpointPositions = new List<System.Numerics.Vector3>();
        checkpointDistances = new List<float>();
        
        // Get raceline data from ACOTrackMaster
        List<System.Numerics.Vector2> raceline = ACOTrackMaster.GetCurrentRaceline();
        if (raceline == null || raceline.Count == 0)
        {
            Debug.LogError("No raceline data available for distance-based checkpoints!");
            useDistanceBasedCheckpoints = false;
            return;
        }
        
        // Calculate total raceline distance
        totalRacelineDistance = 0f;
        Vector3 previousPoint = new Vector3(raceline[0].X, 0, raceline[0].Y);

        for (int i = 1; i < raceline.Count; i++)
        {
            Vector3 currentPoint = new Vector3(raceline[i].X, 0, raceline[i].Y);
            totalRacelineDistance += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        
        // Distribute checkpoints evenly along the raceline
        int totalCheckpoints = allCheckpoints.Count;
        float distancePerCheckpoint = totalRacelineDistance / totalCheckpoints;
        
        float currentDistance = 0f;
        int racelineIndex = 0;
        System.Numerics.Vector3 lastRacelinePoint = new System.Numerics.Vector3(raceline[0].X, 0, raceline[0].Y);
        
        for (int checkpointIndex = 0; checkpointIndex < totalCheckpoints; checkpointIndex++)
        {
            float targetDistance = checkpointIndex * distancePerCheckpoint;
            
            // Find the raceline point closest to our target distance
            while (currentDistance < targetDistance && racelineIndex < raceline.Count - 1)
            {
                racelineIndex++;
                System.Numerics.Vector3 nextPoint = new System.Numerics.Vector3(raceline[racelineIndex].X, 0, raceline[racelineIndex].Y);
                currentDistance += System.Numerics.Vector3.Distance(lastRacelinePoint, nextPoint);
                lastRacelinePoint = nextPoint;
            }
            
            checkpointPositions.Add(lastRacelinePoint);
            checkpointDistances.Add(targetDistance);
            
            Debug.Log($"Distance-based checkpoint {checkpointIndex} placed at distance {targetDistance:F2} at position {lastRacelinePoint}");
        }
    }

    float GetAgentDistanceAlongRaceline(Vector3 agentPosition)
    {
        var raceline = ACOTrackMaster.GetCurrentRaceline();
        if (raceline == null) return 0f;
        
        // Find closest point on raceline to agent
        float closestDistance = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < raceline.Count; i++)
        {
            Vector3 racelinePoint = new Vector3(raceline[i].X, 0, raceline[i].Y);
            float distance = Vector3.Distance(agentPosition, racelinePoint);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        // Calculate distance along raceline up to closest point
        float distanceAlongRaceline = 0f;
        Vector3 previousPoint = new Vector3(raceline[0].X, 0, raceline[0].Y);

        for (int i = 1; i <= closestIndex; i++)
        {
            Vector3 currentPoint = new Vector3(raceline[i].X, 0, raceline[i].Y);
            distanceAlongRaceline += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        
        return distanceAlongRaceline;
    }

    void CheckDistanceBasedCheckpoints(int agentIndex)
    {
        if (!useDistanceBasedCheckpoints || agents[agentIndex] == null) return;
        
        int agentId = agents[agentIndex].ID;
        System.Numerics.Vector3 agentPosition = new System.Numerics.Vector3(agents[agentIndex].Position.X, 0, agents[agentIndex].Position.Y);
        
        // Initialize agent checkpoint tracking if needed
        if (!agentCurrentCheckpoints.ContainsKey(agentId))
        {
            agentCurrentCheckpoints[agentId] = trainingSessions[currentSessionIndex].startCheckpoint;
            agentCheckpointProgress[agentId] = 0f;
            agentCheckpointTriggered[agentId] = false;
        }
        
        int currentTargetCheckpoint = agentCurrentCheckpoints[agentId];
        
        // Check if agent is close enough to current target checkpoint
        if (currentTargetCheckpoint < checkpointPositions.Count)
        {
            System.Numerics.Vector3 checkpointPos = checkpointPositions[currentTargetCheckpoint];
            float distanceToCheckpoint = System.Numerics.Vector3.Distance(agentPosition, checkpointPos);
            
            // Update progress tracking
            agentCheckpointProgress[agentId] = distanceToCheckpoint;
            
            // Check if checkpoint should be triggered
            if (distanceToCheckpoint <= checkpointTriggerDistance && !agentCheckpointTriggered[agentId])
            {
                agentCheckpointTriggered[agentId] = true;
                TriggerDistanceBasedCheckpoint(agentIndex, currentTargetCheckpoint);
                
                // Move to next checkpoint
                agentCurrentCheckpoints[agentId] = (currentTargetCheckpoint + 1) % checkpointPositions.Count;
            }
            else if (distanceToCheckpoint > checkpointTriggerDistance * 2f)
            {
                // Reset trigger flag if agent moves away
                agentCheckpointTriggered[agentId] = false;
            }
        }
    }

    void TriggerDistanceBasedCheckpoint(int agentIndex, int checkpointId)
    {
        // This replaces the collision-based OnCheckpointTriggered logic
        if (!isTraining) return;
        
        var session = trainingSessions[currentSessionIndex];
        
        if (checkpointId == session.goalCheckpoint)
        {
            // Agent reached goal checkpoint - capture state for next session
            agentGoalStates[agentIndex] = new ACOAgentState(
                agents[agentIndex].Position,
                agents[agentIndex].Forward,
                agents[agentIndex].GetCurrentSpeed(),
                agents[agentIndex].GetCurrentTurnAngle()
            );
            agentGoalStates[agentIndex].inputSequence = new List<(int, int)>(agentStates[agentIndex].inputSequence);
            
            float timeToGoal = Time.time - agentStartTimes[agentIndex];
            // Debug.Log($"Agent {agentIndex} reached goal checkpoint in {timeToGoal:F2}s at speed {agentGoalStates[agentIndex].speed:F2} m/s, continuing to validate");
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
                    session.bestState = new ACOAgentState(
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
                    session.bestState = new ACOAgentState(state.position, state.forward, state.speed, state.turnAngle);
                    session.bestState.inputSequence = new List<(int, int)>(state.inputSequence);
                }
                
                session.bestState.timeToGoal = totalTime;
                session.bestState.isValid = true;
                
                Debug.Log($"NEW BEST COMPLETE TIME for session {currentSessionIndex}: {totalTime:F2}s");
            }
            
            RecycleAgent(agentIndex, true, $"Completed full run in {totalTime:F2}s");
        }
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
    
    // Threading configuration methods
    public void SetThreadCount(int count)
    {
        if (!isTraining && count > 0)
        {
            threadCount = Mathf.Clamp(count, 1, 8);
            Debug.Log($"Thread count set to: {threadCount}");
        }
    }
    
    public void SetAgentsPerThread(int count)
    {
        if (!isTraining && count > 0)
        {
            agentsPerThread = Mathf.Clamp(count, 1, 10);
            Debug.Log($"Agents per thread set to: {agentsPerThread}");
        }
    }
    
    public void ToggleMultithreading()
    {
        if (!isTraining)
        {
            enableMultithreading = !enableMultithreading;
            Debug.Log($"Multithreading: {(enableMultithreading ? "Enabled" : "Disabled")}");
        }
    }
    
    // Public accessors for threading
    public bool EnableMultithreading => enableMultithreading;
    public int ThreadCount => threadCount;
    public int AgentsPerThread => agentsPerThread;
    
    // Distance-based checkpoint system accessors
    public bool UseDistanceBasedCheckpoints => useDistanceBasedCheckpoints;
    public float CheckpointTriggerDistance => checkpointTriggerDistance;
    public List<System.Numerics.Vector3> CheckpointPositions => checkpointPositions;
    public Dictionary<int, int> AgentCurrentCheckpoints => agentCurrentCheckpoints;
    public Dictionary<int, float> AgentCheckpointProgress => agentCheckpointProgress;
    
    public void SetCheckpointTriggerDistance(float distance)
    {
        checkpointTriggerDistance = Mathf.Max(0.1f, distance);
        Debug.Log($"Checkpoint trigger distance set to: {checkpointTriggerDistance:F1}");
    }
    
    public void SetTotalCheckpoints(int count)
    {
        if (!isTraining && count >= 3)
        {
            totalCheckpoints = count;
            Debug.Log($"Total checkpoints set to: {totalCheckpoints}");
        }
        else if (count < 3)
        {
            Debug.LogWarning("Total checkpoints must be at least 3!");
        }
    }
    
    public void ToggleDistanceBasedCheckpoints()
    {
        useDistanceBasedCheckpoints = !useDistanceBasedCheckpoints;
        Debug.Log($"Distance-based checkpoints: {(useDistanceBasedCheckpoints ? "Enabled" : "Disabled")}");
        
        if (useDistanceBasedCheckpoints && checkpointPositions == null)
        {
            InitializeDistanceBasedCheckpoints();
        }
    }
    
    #endregion

    void Clear()
    {
        foreach (var agent in agents)
        {
            if (agent != null)
            {
                // Destroy the component GameObject and remove from dictionary
                if (agentComponents.ContainsKey(agent.ID))
                {
                    Destroy(agentComponents[agent.ID].gameObject);
                    agentComponents.Remove(agent.ID);
                }
            }
        }
        
        agents.Clear();
        agentStates.Clear();
        agentGoalStates.Clear();
        agentCheckpointTargets.Clear();
        agentStartTimes.Clear();
        availableAgentSlots.Clear();
        agentComponents.Clear();
        
        // Clear distance-based checkpoint tracking
        if (useDistanceBasedCheckpoints)
        {
            agentCurrentCheckpoints?.Clear();
            agentCheckpointProgress?.Clear();
            agentCheckpointTriggered?.Clear();
        }
        
        // Clear threading infrastructure
        if (enableMultithreading)
        {
            // Stop all worker threads
            if (workerThreads != null)
            {
                foreach (var worker in workerThreads)
                {
                    worker.StopThread();
                }
                workerThreads.Clear();
            }
            
            // Clear concurrent collections (buffers are cleared by individual worker threads)
            if (agentEventQueue != null)
            {
                while (agentEventQueue.TryDequeue(out _)) { }
            }
        }
        
        activeAgents = 0;
        currentIteration = 0;
        totalAttempts = 0;
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
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i] != null)
                {
                    agents[i].Step();
                    
                    // Sync component transform after stepping
                    if (agentComponents.ContainsKey(agents[i].ID))
                    {
                        var component = agentComponents[agents[i].ID];
                        component.transform.position = new Vector3(agents[i].Position.X, component.transform.position.y, agents[i].Position.Y);
                        
                        Vector3 forward3D = new Vector3(agents[i].Forward.X, 0, agents[i].Forward.Y);
                        if (forward3D.magnitude > 0.001f)
                        {
                            component.transform.rotation = Quaternion.LookRotation(forward3D);
                        }
                    }
                }
            }
        }
    }
    
    // Debug visualization for distance-based checkpoints
    void OnDrawGizmos()
    {
        if (!useDistanceBasedCheckpoints || checkpointPositions == null) return;
        
        // Draw checkpoint positions
        for (int i = 0; i < checkpointPositions.Count; i++)
        {
            System.Numerics.Vector3 posTemp = checkpointPositions[i];
            Vector3 pos = new Vector3(posTemp.X, 0, posTemp.Z);
            
            // Color-code checkpoints based on current session
            if (currentSessionIndex < trainingSessions.Count)
            {
                var session = trainingSessions[currentSessionIndex];
                if (i == session.startCheckpoint)
                    Gizmos.color = Color.green;
                else if (i == session.goalCheckpoint)
                    Gizmos.color = Color.yellow;
                else if (i == session.validateCheckpoint)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.white;
            }
            else
            {
                Gizmos.color = Color.white;
            }
            
            // Draw checkpoint sphere
            Gizmos.DrawWireSphere(pos, checkpointTriggerDistance);
            Gizmos.DrawSphere(pos, 1f);
            
            // Draw checkpoint number (approximation using lines)
            Gizmos.color = Color.black;
            Gizmos.DrawLine(pos + Vector3.up * 3f, pos + Vector3.up * 5f);
        }
        
        // Draw agent progress to checkpoints
        if (Application.isPlaying && agents != null)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i] != null && agentCurrentCheckpoints.ContainsKey(agents[i].ID))
                {
                    Vector3 agentPos = new Vector3(agents[i].Position.X, 0, agents[i].Position.Y);
                    int targetCheckpoint = agentCurrentCheckpoints[agents[i].ID];
                    
                    if (targetCheckpoint < checkpointPositions.Count)
                    {
                        Vector3 checkpointPos = new Vector3(checkpointPositions[targetCheckpoint].X, 0, checkpointPositions[targetCheckpoint].Z);
                        
                        // Draw line from agent to target checkpoint
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(agentPos, checkpointPos);
                        
                        // Draw agent position
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireSphere(agentPos, 0.5f);
                    }
                    
                    // DEBUG: Draw Forward vector for each agent
                    System.Numerics.Vector2 forward = agents[i].Forward;
                    Vector3 forwardVector3D = new Vector3(forward.X, 0, forward.Y);
                    Vector3 forwardEnd = agentPos + forwardVector3D * 3f; // Scale by 3 for visibility
                    
                    // Draw forward direction as red arrow
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(agentPos, forwardEnd);
                    
                    // Draw arrowhead
                    Vector3 arrowHead1 = forwardEnd - forwardVector3D.normalized * 0.5f + Vector3.Cross(forwardVector3D, Vector3.up).normalized * 0.3f;
                    Vector3 arrowHead2 = forwardEnd - forwardVector3D.normalized * 0.5f - Vector3.Cross(forwardVector3D, Vector3.up).normalized * 0.3f;
                    Gizmos.DrawLine(forwardEnd, arrowHead1);
                    Gizmos.DrawLine(forwardEnd, arrowHead2);
                }
            }
        }
    }
}
