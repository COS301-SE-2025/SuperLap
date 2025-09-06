using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Numerics;
using UnityEngine;
using System;

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
    public ThreadSafeAgentState FinalState { get; set; }
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
    
    // Shared data (read-only)
    private PolygonTrack track;
    private ACOAgentState initialTrainingState;
    private ACOTrainingSession currentSession;
    private float checkpointTriggerDistance;
    private List<UnityEngine.Vector3> checkpointPositions;
    
    // Communication with main thread
    private WorkerThreadBuffer agentBuffer;
    private ConcurrentQueue<AgentEvent> eventQueue;
    
    public ACOWorkerThread(
        WorkerThreadBuffer buffer,
        ConcurrentQueue<AgentEvent> events,
        PolygonTrack trackData,
        int decisionInt)
    {
        agentBuffer = buffer;
        eventQueue = events;
        track = trackData;
        decisionInterval = decisionInt;
        
        agents = new List<ACOAgent>();
        agentStates = new List<ACOAgentState>();
        agentStartTimes = new List<float>();
        agentCheckpointTargets = new List<int>();
        agentActive = new List<bool>();
        
        pauseEvent = new ManualResetEventSlim(true);
        currentStep = 0;
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
        
        // Create new ACOAgent
        ACOAgent agent = new ACOAgent(track, initialTrainingState.position, bearing);
        agent.SetInitialState(initialTrainingState.speed, initialTrainingState.turnAngle);
        
                // Add to collections
        agents.Add(agent);
        agentStates.Add(new ACOAgentState(initialTrainingState.position, initialTrainingState.forward, 
                                         initialTrainingState.speed, initialTrainingState.turnAngle));
        agentStartTimes.Add(DateTime.UtcNow.Millisecond);
        agentCheckpointTargets.Add(currentSession.validateCheckpoint);
        agentActive.Add(true);
        
        Debug.Log($"Worker thread spawned agent {agentId}");
    }
    
    private void ThreadMain()
    {
        try
        {
            while (!shouldStop)
            {
                // Wait if paused
                pauseEvent.Wait();
                
                if (shouldStop) break;
                
                // Update all active agents
                for (int i = agents.Count - 1; i >= 0; i--)
                {
                    if (agentActive[i])
                    {
                        UpdateAgent(i);
                    }
                }
                
                // Write all agent states to buffer
                WriteAgentStatesToBuffer();
                
                currentStep++;
                
                // Simple frame rate limiting (30 FPS)
                Thread.Sleep(33);
            }
        }
        catch (System.Exception e)
        {
            throw new Exception($"Worker thread error: {e.Message}");
        }
        
    }
    
    private void UpdateAgent(int agentIndex)
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
        if (agent.IsOffTrack())
        {
            RecycleAgent(agentIndex, false, "Went off track");
            return;
        }
        
        // Check distance-based checkpoints
        CheckDistanceBasedCheckpoints(agentIndex);
        
        // Update agent state
        state.position = agent.Position;
        state.forward = agent.Forward;
        state.speed = agent.GetCurrentSpeed();
        state.turnAngle = agent.GetCurrentTurnAngle();
        
        // Note: We'll batch write all states to buffer in WriteAgentStatesToBuffer()
    }
    
    private void CheckDistanceBasedCheckpoints(int agentIndex)
    {
        if (checkpointPositions == null || agents[agentIndex] == null) return;
        
        int agentId = agents[agentIndex].ID;
        UnityEngine.Vector3 agentPosition = new UnityEngine.Vector3(agents[agentIndex].Position.X, 0, agents[agentIndex].Position.Y);
        
        // Simple checkpoint checking - check goal and validate checkpoints
        if (currentSession != null)
        {
            // Check goal checkpoint
            if (currentSession.goalCheckpoint < checkpointPositions.Count)
            {
                UnityEngine.Vector3 goalPos = checkpointPositions[currentSession.goalCheckpoint];
                float distanceToGoal = UnityEngine.Vector3.Distance(agentPosition, goalPos);
                
                if (distanceToGoal <= checkpointTriggerDistance)
                {
                    // Agent reached goal checkpoint
                    var goalEvent = new AgentEvent
                    {
                        EventType = AgentEventType.CheckpointTriggered,
                        AgentId = agentId,
                        CheckpointId = currentSession.goalCheckpoint,
                        CompletionTime = DateTime.UtcNow.Millisecond - agentStartTimes[agentIndex]
                    };
                    eventQueue.Enqueue(goalEvent);
                }
            }
            
            // Check validate checkpoint
            if (currentSession.validateCheckpoint < checkpointPositions.Count)
            {
                UnityEngine.Vector3 validatePos = checkpointPositions[currentSession.validateCheckpoint];
                float distanceToValidate = UnityEngine.Vector3.Distance(agentPosition, validatePos);
                
                if (distanceToValidate <= checkpointTriggerDistance)
                {
                    // Agent completed the run
                    float totalTime = DateTime.UtcNow.Millisecond - agentStartTimes[agentIndex];
                    var state = agentStates[agentIndex];
                    state.timeToGoal = totalTime;
                    state.isValid = true;
                    
                    var completeEvent = new AgentEvent
                    {
                        EventType = AgentEventType.AgentCompleted,
                        AgentId = agentId,
                        CheckpointId = currentSession.validateCheckpoint,
                        CompletionTime = totalTime,
                        FinalState = new ThreadSafeAgentState(agentId)
                    };
                    completeEvent.FinalState.UpdateTrainingData(state.inputSequence, totalTime, true);
                    
                    eventQueue.Enqueue(completeEvent);
                    RecycleAgent(agentIndex, true, $"Completed in {totalTime:F2}s");
                }
            }
        }
    }
    
    private void RecycleAgent(int agentIndex, bool wasSuccessful, string reason)
    {
        if (agentIndex < 0 || agentIndex >= agents.Count) return;
        
        int agentId = agents[agentIndex].ID;
        
        // Note: Agent will be marked inactive in buffer during next WriteAgentStatesToBuffer() call
        
        // Send failure event if not successful
        if (!wasSuccessful)
        {
            var failEvent = new AgentEvent
            {
                EventType = AgentEventType.AgentFailed,
                AgentId = agentId,
                Reason = reason
            };
            eventQueue.Enqueue(failEvent);
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
        var writeBuffer = agentBuffer.GetWriteBuffer();
        
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
        
        // Set the active count and commit the buffer
        writeBuffer.SetActiveCount(agents.Count);
        agentBuffer.CommitData();
    }
}