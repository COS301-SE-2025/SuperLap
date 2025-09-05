using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Worker thread pool for parallel agent simulation
/// Manages a pool of worker threads that process agent simulation work items
/// </summary>
public class WorkerThreadPool : IDisposable
{
    private readonly List<WorkerThread> workers;
    private readonly ConcurrentQueue<WorkItemWrapper> workQueue;
    private readonly CancellationToken cancellationToken;
    private readonly ManualResetEventSlim workAvailable;
    private volatile bool isRunning;
    
    public WorkerThreadPool(int threadCount, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        this.workQueue = new ConcurrentQueue<WorkItemWrapper>();
        this.workAvailable = new ManualResetEventSlim(false);
        this.workers = new List<WorkerThread>();
        this.isRunning = true;
        
        // Create and start worker threads
        for (int i = 0; i < threadCount; i++)
        {
            var worker = new WorkerThread(i, workQueue, workAvailable, cancellationToken);
            workers.Add(worker);
            worker.Start();
        }
    }
    
    /// <summary>
    /// Submit work to be processed by a worker thread
    /// Returns a task that completes when the work is done
    /// </summary>
    public Task<AgentSimulationResult> SubmitWork(AgentWorkItem workItem)
    {
        if (!isRunning)
            throw new InvalidOperationException("Worker pool is not running");
        
        var tcs = new TaskCompletionSource<AgentSimulationResult>();
        var wrapper = new WorkItemWrapper(workItem, tcs);
        
        workQueue.Enqueue(wrapper);
        workAvailable.Set();
        
        return tcs.Task;
    }
    
    /// <summary>
    /// Stop all worker threads
    /// </summary>
    public void Stop()
    {
        isRunning = false;
        workAvailable.Set(); // Wake up any waiting threads
        
        foreach (var worker in workers)
        {
            worker.Stop();
        }
    }
    
    public void Dispose()
    {
        Stop();
        workAvailable?.Dispose();
    }
    
    /// <summary>
    /// Internal wrapper for work items with completion tracking
    /// </summary>
    private class WorkItemWrapper
    {
        public AgentWorkItem WorkItem { get; }
        public TaskCompletionSource<AgentSimulationResult> CompletionSource { get; }
        
        public WorkItemWrapper(AgentWorkItem workItem, TaskCompletionSource<AgentSimulationResult> completionSource)
        {
            WorkItem = workItem;
            CompletionSource = completionSource;
        }
    }
    
    /// <summary>
    /// Individual worker thread that processes simulation work
    /// </summary>
    private class WorkerThread
    {
        private readonly int threadId;
        private readonly ConcurrentQueue<WorkItemWrapper> workQueue;
        private readonly ManualResetEventSlim workAvailable;
        private readonly CancellationToken cancellationToken;
        private readonly Thread thread;
        private volatile bool isRunning;
        
        public WorkerThread(int threadId, ConcurrentQueue<WorkItemWrapper> workQueue, 
                          ManualResetEventSlim workAvailable, CancellationToken cancellationToken)
        {
            this.threadId = threadId;
            this.workQueue = workQueue;
            this.workAvailable = workAvailable;
            this.cancellationToken = cancellationToken;
            this.thread = new Thread(WorkerLoop) { Name = $"AgentWorker-{threadId}", IsBackground = true };
            this.isRunning = true;
        }
        
        public void Start()
        {
            thread.Start();
        }
        
        public void Stop()
        {
            isRunning = false;
        }
        
        private void WorkerLoop()
        {
            while (isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for work to become available
                    workAvailable.Wait(100, cancellationToken);
                    
                    // Process all available work
                    while (workQueue.TryDequeue(out WorkItemWrapper wrapper) && isRunning)
                    {
                        try
                        {
                            var result = ProcessWorkItem(wrapper.WorkItem);
                            wrapper.CompletionSource.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            wrapper.CompletionSource.SetException(ex);
                        }
                    }
                    
                    // Reset signal if queue is empty
                    if (workQueue.IsEmpty)
                    {
                        workAvailable.Reset();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log error and continue
                    Console.WriteLine($"Worker thread {threadId} error: {ex.Message}");
                }
            }
        }
        
        private AgentSimulationResult ProcessWorkItem(AgentWorkItem workItem)
        {
            return workItem.Execute();
        }
    }
}

/// <summary>
/// Work item representing a batch of agents to simulate
/// </summary>
public class AgentWorkItem
{
    private readonly List<LightweightAgent> agents;
    private readonly TrainingSessionThreaded session;
    private readonly TrackMeshSystem trackMesh;
    private readonly DistanceCheckpointSystem checkpointSystem;
    private readonly TrainingConfiguration config;
    
    public AgentWorkItem(List<LightweightAgent> agents, TrainingSessionThreaded session, 
                        TrackMeshSystem trackMesh, DistanceCheckpointSystem checkpointSystem, 
                        TrainingConfiguration config)
    {
        this.agents = agents;
        this.session = session;
        this.trackMesh = trackMesh;
        this.checkpointSystem = checkpointSystem;
        this.config = config;
    }
    
    /// <summary>
    /// Execute the simulation for all agents in this work item
    /// </summary>
    public AgentSimulationResult Execute()
    {
        var result = new AgentSimulationResult();
        var agentVisualizationData = new List<AgentVisualizationData>();
        
        foreach (var agent in agents)
        {
            // Reset checkpoint system for this agent
            checkpointSystem.ResetAgent(agent.state.agentId);
            
            // Run simulation for this agent
            var agentResult = SimulateAgent(agent);
            
            // Aggregate results
            result.Merge(agentResult);
            
            // Add visualization data
            agentVisualizationData.Add(new AgentVisualizationData(
                agent.state, 
                agent.IsOffTrack(trackMesh), 
                checkpointSystem.GetProgressPercentage(agent.state.agentId)
            ));
        }
        
        result.AgentVisualizationData = agentVisualizationData;
        return result;
    }
    
    private SingleAgentResult SimulateAgent(LightweightAgent agent)
    {
        float simulationTime = 0f;
        float deltaTime = 0.02f; // 50 FPS simulation
        int steps = 0;
        bool completedObjective = false;
        bool wentOffTrack = false;
        List<AgentInput> inputSequence = new List<AgentInput>();
        
        while (simulationTime < config.MaxSimulationTime && agent.state.isActive && !completedObjective)
        {
            // Make decision
            AgentInput decision = agent.Decide(trackMesh, checkpointSystem);
            inputSequence.Add(decision);
            
            // Simulate one step
            agent.Step(deltaTime * config.TimeMultiplier, trackMesh, checkpointSystem);
            
            // Check for completion conditions
            if (agent.IsOffTrack(trackMesh))
            {
                wentOffTrack = true;
                break;
            }
            
            // Check if reached validation checkpoint
            if (agent.state.currentTargetCheckpoint == session.validateCheckpoint && 
                checkpointSystem.GetCheckpointsCompleted(agent.state.agentId) >= 2)
            {
                completedObjective = true;
            }
            
            simulationTime += deltaTime;
            steps++;
        }
        
        return new SingleAgentResult
        {
            AgentId = agent.state.agentId,
            IsValid = completedObjective && !wentOffTrack,
            CompletionTime = simulationTime,
            CheckpointsCompleted = checkpointSystem.GetCheckpointsCompleted(agent.state.agentId),
            TotalDistance = agent.state.totalDistance,
            InputSequence = inputSequence,
            PerformanceMetrics = agent.GetPerformanceMetrics(),
            Steps = steps
        };
    }
}

/// <summary>
/// Result from simulating a batch of agents
/// </summary>
public class AgentSimulationResult
{
    public int AgentId { get; set; }
    public bool IsValid { get; set; }
    public float CompletionTime { get; set; }
    public int CheckpointsCompleted { get; set; }
    public float TotalDistance { get; set; }
    public List<AgentInput> InputSequence { get; set; } = new List<AgentInput>();
    public AgentPerformanceMetrics PerformanceMetrics { get; set; }
    public int TotalSteps { get; set; }
    public List<AgentVisualizationData> AgentVisualizationData { get; set; } = new List<AgentVisualizationData>();
    
    /// <summary>
    /// Merge results from multiple agents
    /// </summary>
    public void Merge(SingleAgentResult other)
    {
        // For batch results, we keep the best performing agent as the primary result
        if (other.IsValid && (!IsValid || other.CompletionTime < CompletionTime))
        {
            AgentId = other.AgentId;
            IsValid = other.IsValid;
            CompletionTime = other.CompletionTime;
            CheckpointsCompleted = other.CheckpointsCompleted;
            TotalDistance = other.TotalDistance;
            InputSequence = other.InputSequence;
            PerformanceMetrics = other.PerformanceMetrics;
        }
        
        TotalSteps += other.Steps;
    }
}

/// <summary>
/// Result from simulating a single agent
/// </summary>
public class SingleAgentResult
{
    public int AgentId { get; set; }
    public bool IsValid { get; set; }
    public float CompletionTime { get; set; }
    public int CheckpointsCompleted { get; set; }
    public float TotalDistance { get; set; }
    public List<AgentInput> InputSequence { get; set; }
    public AgentPerformanceMetrics PerformanceMetrics { get; set; }
    public int Steps { get; set; }
}