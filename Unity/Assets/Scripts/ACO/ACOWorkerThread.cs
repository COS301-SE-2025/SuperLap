using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Numerics;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;

public enum AgentEventType
{
    CheckpointTriggered,
    AgentCompleted,
    AgentFailed
}

public class AgentEvent
{
    public AgentEventType EventType { get; set; }
    public int AgentId { get; set; }
    public int CheckpointId { get; set; }
    public string Reason { get; set; }
    public float CompletionTime { get; set; }
    public List<(int, int)> InputSequence { get; set; }
    public bool IsValid { get; set; }
}

public class ACOWorkerThread
{
    private Thread workerThread;
    private volatile bool shouldStop;
    private volatile bool isPaused;
    private ManualResetEventSlim pauseEvent;
    
    // Thread-specific data
    private List<ACOAgent> agents;
    private List<ACOAgentState> agentStates;
    private List<float> agentStartTimes;
    private List<int> agentCheckpointTargets;
    private List<bool> agentActive;
    private int currentStep;
    private int decisionInterval;
    
    // Shared data (read-only) - Thread-local copies to avoid memory contention
    private PolygonTrack track;
    private List<System.Numerics.Vector2> threadLocalRaceline;
    private ThreadLocalRacelineAnalyzer threadLocalRacelineAnalyzer;
    private ACOAgentState initialTrainingState;
    private ACOTrainingSession currentSession;
    private float checkpointTriggerDistance;
    private List<UnityEngine.Vector3> checkpointPositions;
    
    // Communication with main thread
    private WorkerThreadBuffer agentBuffer;
    private ConcurrentQueue<AgentEvent> eventQueue;
    
    // Performance profiling
    private StreamWriter performanceLog;
    private string logFilePath;
    private Stopwatch stopwatch;
    private int threadId;
    
    public ACOWorkerThread(
        WorkerThreadBuffer buffer,
        ConcurrentQueue<AgentEvent> events,
        PolygonTrack trackData,
        List<System.Numerics.Vector2> racelineData,
        int decisionInt,
        int workerId = 0)
    {
        agentBuffer = buffer;
        eventQueue = events;
        
        // Create thread-local copy of track to avoid memory contention
        track = new PolygonTrack(trackData);
        
        // Create deep copy of raceline with proper data type conversion to avoid shared references
        threadLocalRaceline = new List<System.Numerics.Vector2>();
        if (racelineData != null)
        {
            foreach (var point in racelineData)
            {
                threadLocalRaceline.Add(new System.Numerics.Vector2(point.X, point.Y));
            }
        }
        
        // Initialize thread-local raceline analyzer to avoid shared memory contention
        threadLocalRacelineAnalyzer = new ThreadLocalRacelineAnalyzer();
        threadLocalRacelineAnalyzer.InitializeWithRaceline(threadLocalRaceline);
        
        decisionInterval = decisionInt;
        threadId = workerId;
        
        agents = new List<ACOAgent>();
        agentStates = new List<ACOAgentState>();
        agentStartTimes = new List<float>();
        agentCheckpointTargets = new List<int>();
        agentActive = new List<bool>();
        
        pauseEvent = new ManualResetEventSlim(true);
        currentStep = 0;
        stopwatch = new Stopwatch();
        
        // Create performance log file
        InitializePerformanceLogging();
    }
    
    public void StartThread()
    {
        if (workerThread == null || !workerThread.IsAlive)
        {
            shouldStop = false;
            workerThread = new Thread(ThreadMain) { IsBackground = true };
            workerThread.Start();
        }
    }
    
    public void StopThread()
    {
        shouldStop = true;
        pauseEvent.Set(); // Wake up if paused
        
        if (workerThread != null && workerThread.IsAlive)
        {
            workerThread.Join(1000); // Wait 1 second for graceful shutdown
        }
        
        // Close performance log
        CleanupPerformanceLogging();
    }
    
    private void InitializePerformanceLogging()
    {
        try
        {
            string logDirectory = Path.Combine("/home/richter/ACOPerformanceLogs");
            Directory.CreateDirectory(logDirectory);
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logDirectory, $"WorkerThread_{threadId}_{timestamp}.log");
            
            performanceLog = new StreamWriter(logFilePath, false);
            performanceLog.WriteLine($"Performance Log for Worker Thread {threadId}");
            performanceLog.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            performanceLog.WriteLine("Timestamp(ms),Event,Duration(ms),Details");
            performanceLog.Flush();
            
            UnityEngine.Debug.Log($"Worker thread {threadId} performance logging initialized: {logFilePath}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to initialize performance logging for thread {threadId}: {e.Message}");
        }
    }
    
    private void CleanupPerformanceLogging()
    {
        try
        {
            if (performanceLog != null)
            {
                performanceLog.WriteLine($"Log ended at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                performanceLog.Close();
                performanceLog.Dispose();
                performanceLog = null;
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error closing performance log for thread {threadId}: {e.Message}");
        }
    }
    
    private void LogPerformanceEvent(string eventName, double durationMs, string details = "")
    {
        try
        {
            if (performanceLog != null)
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                performanceLog.WriteLine($"{timestamp},{eventName},{durationMs:F3},{details}");
                performanceLog.Flush();
            }
        }
        catch (Exception)
        {
            // Don't log errors to avoid infinite recursion
        }
    }
    
    public void PauseThread()
    {
        isPaused = true;
        pauseEvent.Reset();
    }
    
    public void ResumeThread()
    {
        isPaused = false;
        pauseEvent.Set();
    }
    
    public void SetTrainingData(ACOAgentState trainingState, ACOTrainingSession session, List<UnityEngine.Vector3> checkpoints, float triggerDistance)
    {
        initialTrainingState = trainingState;
        currentSession = session;
        checkpointPositions = checkpoints;
        checkpointTriggerDistance = triggerDistance;
    }
    
    public void SpawnAgent(int agentId)
    {
        if (initialTrainingState == null) return;
        
        // Calculate bearing from forward vector
        float bearing = CalculateBearing(initialTrainingState.forward);
        
        // Create new ACOAgent with thread-local track and raceline copies, using provided agentId
        ACOAgent agent = new ACOAgent(track, initialTrainingState.position, bearing, threadLocalRaceline, threadLocalRacelineAnalyzer, agentId);
        agent.SetInitialState(initialTrainingState.speed, initialTrainingState.turnAngle);
        
        // Add to collections
        agents.Add(agent);
        agentStates.Add(new ACOAgentState(initialTrainingState.position, initialTrainingState.forward, 
                                         initialTrainingState.speed, initialTrainingState.turnAngle));
        agentStartTimes.Add(System.Environment.TickCount); // Use TickCount instead of DateTime for better threading performance
        agentCheckpointTargets.Add(currentSession.validateCheckpoint);
        agentActive.Add(true);
        
        // UnityEngine.Debug.Log($"Worker thread spawned agent {agentId}"); // DISABLED: Debug.Log causes thread contention
    }
    
    private void ThreadMain()
    {
        try
        {
            LogPerformanceEvent("ThreadMain_Start", 0, $"Thread {threadId} starting main loop");
            
            while (!shouldStop)
            {
                stopwatch.Restart();
                
                // Wait if paused
                pauseEvent.Wait();
                
                if (shouldStop) break;
                
                double pauseWaitTime = stopwatch.Elapsed.TotalMilliseconds;
                if (pauseWaitTime > 1.0) // Only log if we actually waited
                {
                    LogPerformanceEvent("PauseWait", pauseWaitTime, "Waiting for pause event");
                }
                
                stopwatch.Restart();
                
                // Update all active agents
                for (int i = agents.Count - 1; i >= 0; i--)
                {
                    if (agentActive[i])
                    {
                        UpdateAgent(i);
                    }
                }
                
                double agentUpdateTime = stopwatch.Elapsed.TotalMilliseconds;
                LogPerformanceEvent("AgentUpdate", agentUpdateTime, $"Updated {agents.Count} agents");
                
                // Write all agent states to buffer
                stopwatch.Restart();
                WriteAgentStatesToBuffer();
                double bufferWriteTime = stopwatch.Elapsed.TotalMilliseconds;
                LogPerformanceEvent("BufferWrite", bufferWriteTime, $"Wrote {agents.Count} agent states");
                
                currentStep++;
                
                // Simple frame rate limiting (30 FPS)
                Thread.Sleep(33);
            }
            
            LogPerformanceEvent("ThreadMain_End", 0, $"Thread {threadId} ending main loop");
        }
        catch (System.Exception e)
        {
            LogPerformanceEvent("ThreadMain_Error", 0, $"Worker thread error: {e.Message}");
            throw new Exception($"Worker thread error: {e.Message}");
        }
        
    }
    
    private void UpdateAgent(int agentIndex)
    {
        var agent = agents[agentIndex];
        var state = agentStates[agentIndex];
        
        if (agent == null || state == null) return;
        
        // Profile agent step operation
        stopwatch.Restart();
        agent.Step();
        double stepTime = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceEvent("Agent_Step", stepTime, $"Agent {agent.ID} step operation");
        
        // Profile decision making
        if ((currentStep + agentIndex) % decisionInterval == 0)
        {
            stopwatch.Restart();
            var action = agent.Decide();
            double decideTime = stopwatch.Elapsed.TotalMilliseconds;
            
            stopwatch.Restart();
            agent.SetInput(action.Item1, action.Item2);
            double setInputTime = stopwatch.Elapsed.TotalMilliseconds;
            
            state.inputSequence.Add(action);
            
                LogPerformanceEvent("Agent_Decide", decideTime, $"Agent {agent.ID} decision making");
            
                LogPerformanceEvent("Agent_SetInput", setInputTime, $"Agent {agent.ID} input setting");
        }
        
        // Profile off-track checking
        stopwatch.Restart();
        bool isOffTrack = agent.IsOffTrack();
        double offTrackTime = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceEvent("Agent_OffTrackCheck", offTrackTime, $"Agent {agent.ID} off-track check");
        
        if (isOffTrack)
        {
            RecycleAgent(agentIndex, false, "Went off track");
            return;
        }
        
        // Profile checkpoint checking
        stopwatch.Restart();
        CheckDistanceBasedCheckpoints(agentIndex);
        double checkpointTime = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceEvent("Agent_CheckpointCheck", checkpointTime, $"Agent {agent.ID} checkpoint checking");
        
        // Profile state updates
        stopwatch.Restart();
        state.position = agent.Position;
        state.forward = agent.Forward;
        state.speed = agent.GetCurrentSpeed();
        state.turnAngle = agent.GetCurrentTurnAngle();
        double stateUpdateTime = stopwatch.Elapsed.TotalMilliseconds;

            LogPerformanceEvent("Agent_StateUpdate", stateUpdateTime, $"Agent {agent.ID} state update");
        
        // Note: We'll batch write all states to buffer in WriteAgentStatesToBuffer()
    }
    
    private void CheckDistanceBasedCheckpoints(int agentIndex)
    {
        if (checkpointPositions == null || agents[agentIndex] == null) return;
        
        stopwatch.Restart();
        
        int agentId = agents[agentIndex].ID;
        UnityEngine.Vector3 agentPosition = new UnityEngine.Vector3(agents[agentIndex].Position.X, 0, agents[agentIndex].Position.Y);
        
        double setupTime = stopwatch.Elapsed.TotalMilliseconds;
        
        // Simple checkpoint checking - check goal and validate checkpoints
        if (currentSession != null)
        {
            // Check goal checkpoint
            if (currentSession.goalCheckpoint < checkpointPositions.Count)
            {
                stopwatch.Restart();
                UnityEngine.Vector3 goalPos = checkpointPositions[currentSession.goalCheckpoint];
                float distanceToGoal = UnityEngine.Vector3.Distance(agentPosition, goalPos);
                double goalDistanceCalcTime = stopwatch.Elapsed.TotalMilliseconds;
                
                    LogPerformanceEvent("Checkpoint_GoalDistance", goalDistanceCalcTime, $"Agent {agentId} goal distance calculation");
                
                if (distanceToGoal <= checkpointTriggerDistance)
                {
                    // Agent reached goal checkpoint
                    stopwatch.Restart();
                    
                    var goalEvent = new AgentEvent
                    {
                        EventType = AgentEventType.CheckpointTriggered,
                        AgentId = agentId,
                        CheckpointId = currentSession.goalCheckpoint,
                        CompletionTime = System.Environment.TickCount - agentStartTimes[agentIndex]
                    };
                    
                    double eventCreationTime = stopwatch.Elapsed.TotalMilliseconds;
                    
                    stopwatch.Restart();
                    eventQueue.Enqueue(goalEvent);
                    double enqueueTime = stopwatch.Elapsed.TotalMilliseconds;
                    
                    LogPerformanceEvent("EventQueue_Enqueue", enqueueTime, $"Goal checkpoint event for agent {agentId}");
                    

                    LogPerformanceEvent("Event_Creation", eventCreationTime, $"Goal event creation for agent {agentId}");
                }
            }
            
            // Check validate checkpoint
            if (currentSession.validateCheckpoint < checkpointPositions.Count)
            {
                stopwatch.Restart();
                UnityEngine.Vector3 validatePos = checkpointPositions[currentSession.validateCheckpoint];
                float distanceToValidate = UnityEngine.Vector3.Distance(agentPosition, validatePos);
                double validateDistanceCalcTime = stopwatch.Elapsed.TotalMilliseconds;
                

                    LogPerformanceEvent("Checkpoint_ValidateDistance", validateDistanceCalcTime, $"Agent {agentId} validate distance calculation");
                
                if (distanceToValidate <= checkpointTriggerDistance)
                {
                    // Agent completed the run
                    float totalTime = System.Environment.TickCount - agentStartTimes[agentIndex];
                    var state = agentStates[agentIndex];
                    state.timeToGoal = totalTime;
                    state.isValid = true;
                    
                    stopwatch.Restart();
                    
                    var completeEvent = new AgentEvent
                    {
                        EventType = AgentEventType.AgentCompleted,
                        AgentId = agentId,
                        CheckpointId = currentSession.validateCheckpoint,
                        CompletionTime = totalTime,
                        InputSequence = new List<(int, int)>(state.inputSequence),
                        IsValid = true
                    };
                    
                    double eventCreationTime = stopwatch.Elapsed.TotalMilliseconds;
                    
                    stopwatch.Restart();
                    eventQueue.Enqueue(completeEvent);
                    double enqueueTime = stopwatch.Elapsed.TotalMilliseconds;
                    
                    LogPerformanceEvent("EventQueue_Enqueue", enqueueTime, $"Completion event for agent {agentId}, time: {totalTime:F2}s");

                    LogPerformanceEvent("Event_Creation", eventCreationTime, $"Completion event creation for agent {agentId}");

                    stopwatch.Restart();
                    RecycleAgent(agentIndex, true, $"Completed in {totalTime:F2}s");
                    double recycleTime = stopwatch.Elapsed.TotalMilliseconds;
                    
                    if (recycleTime > 1.0)
                    {
                        LogPerformanceEvent("Agent_Recycle", recycleTime, $"Recycling completed agent {agentId}");
                    }
                }
            }
        }
        
            LogPerformanceEvent("Checkpoint_Setup", setupTime, $"Agent {agentId} checkpoint setup");
    }
    
    private void RecycleAgent(int agentIndex, bool wasSuccessful, string reason)
    {
        if (agentIndex < 0 || agentIndex >= agents.Count) return;
        
        int agentId = agents[agentIndex].ID;
        
        // Note: Agent will be marked inactive in buffer during next WriteAgentStatesToBuffer() call
        
        // Send failure event if not successful
        if (!wasSuccessful)
        {
            stopwatch.Restart();
            
            var failEvent = new AgentEvent
            {
                EventType = AgentEventType.AgentFailed,
                AgentId = agentId,
                Reason = reason
            };
            eventQueue.Enqueue(failEvent);
            
            double enqueueTime = stopwatch.Elapsed.TotalMilliseconds;
            LogPerformanceEvent("EventQueue_Enqueue", enqueueTime, $"Failure event for agent {agentId}: {reason}");
        }
        
        // Remove from collections
        agents.RemoveAt(agentIndex);
        agentStates.RemoveAt(agentIndex);
        agentStartTimes.RemoveAt(agentIndex);
        agentCheckpointTargets.RemoveAt(agentIndex);
        agentActive.RemoveAt(agentIndex);
    }
    
    private float CalculateBearing(System.Numerics.Vector2 forward)
    {
        float angle = (float)(System.Math.Atan2(forward.Y, forward.X) * 180.0 / System.Math.PI);
        return angle + 90.0f;
    }
    
    public int GetActiveAgentCount()
    {
        return agents.Count;
    }
    
    /// <summary>
    /// Get the buffer for reading agent states (used by main thread)
    /// </summary>
    public WorkerThreadBuffer GetBuffer()
    {
        return agentBuffer;
    }
    
    /// <summary>
    /// Write all agent states to the buffer for main thread consumption
    /// This is called once per frame, dramatically reducing lock contention
    /// </summary>
    private void WriteAgentStatesToBuffer()
    {
        stopwatch.Restart();
        var writeBuffer = agentBuffer.GetWriteBuffer();
        double getWriteBufferTime = stopwatch.Elapsed.TotalMilliseconds;
        
        if (getWriteBufferTime > 1.0) // Only log if it took significant time
        {
            LogPerformanceEvent("GetWriteBuffer", getWriteBufferTime, "Time to get write buffer");
        }
        
        stopwatch.Restart();
        
        // Write all agent states to the buffer
        for (int i = 0; i < agents.Count; i++)
        {
            if (i < agents.Count && agents[i] != null && agentActive[i])
            {
                var agent = agents[i];
                writeBuffer.SetAgentState(
                    i,
                    agent.ID,
                    agent.Position,
                    agent.Forward,
                    agent.GetCurrentSpeed(),
                    agent.GetCurrentTurnAngle(),
                    true,  // active
                    agent.IsOffTrack()
                );
            }
            else
            {
                // Mark slot as inactive
                writeBuffer.DeactivateAgent(i);
            }
        }
        
        // Set the active count
        writeBuffer.SetActiveCount(agents.Count);
        
        double writeTime = stopwatch.Elapsed.TotalMilliseconds;
        LogPerformanceEvent("WriteAgentStates", writeTime, $"Wrote {agents.Count} agent states to buffer");
        
        // This is the critical section - buffer commit with lock
        stopwatch.Restart();
        agentBuffer.CommitData();
        double commitTime = stopwatch.Elapsed.TotalMilliseconds;
        
        LogPerformanceEvent("BufferCommit", commitTime, $"Committed buffer with {agents.Count} agents");
        
        if (commitTime > 5.0) // Log slower commits as warnings
        {
            LogPerformanceEvent("BufferCommit_SLOW", commitTime, $"SLOW buffer commit detected!");
        }
    }
}