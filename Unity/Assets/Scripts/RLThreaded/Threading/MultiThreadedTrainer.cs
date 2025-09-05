using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Multithreaded training system for concurrent agent simulation
/// Core coordinator that manages worker threads and training sessions
/// Thread-safe interface for Unity integration
/// </summary>
public class MultiThreadedTrainer
{
    // Training configuration
    private readonly TrainingConfiguration config;
    private readonly TrackMeshSystem trackMesh;
    private readonly DistanceCheckpointSystem checkpointSystem;
    
    // Threading infrastructure
    private readonly WorkerThreadPool threadPool;
    private readonly CancellationTokenSource cancellationTokenSource;
    
    // Training state (thread-safe)
    private volatile bool isTraining;
    private readonly object stateLock = new object();
    private TrainingProgress currentProgress;
    
    // Training data
    private List<TrainingSessionThreaded> TrainingSessionThreadeds;
    private int currentSessionIndex;
    private ConcurrentQueue<TrainingResult> completedResults;
    private ConcurrentDictionary<int, AgentVisualizationData> activeAgentVisualizations;
    
    // Performance metrics
    private DateTime trainingStartTime;
    private long totalAgentSteps;
    private long totalIterations;
    
    public MultiThreadedTrainer(TrainingConfiguration config, TrackMeshSystem trackMesh, 
                              DistanceCheckpointSystem checkpointSystem)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.trackMesh = trackMesh ?? throw new ArgumentNullException(nameof(trackMesh));
        this.checkpointSystem = checkpointSystem ?? throw new ArgumentNullException(nameof(checkpointSystem));
        
        this.cancellationTokenSource = new CancellationTokenSource();
        this.threadPool = new WorkerThreadPool(config.ThreadCount, cancellationTokenSource.Token);
        this.completedResults = new ConcurrentQueue<TrainingResult>();
        this.activeAgentVisualizations = new ConcurrentDictionary<int, AgentVisualizationData>();
        
        InitializeTrainingSessionThreadeds();
    }
    
    /// <summary>
    /// Start the training process
    /// </summary>
    public void StartTraining()
    {
        lock (stateLock)
        {
            if (isTraining)
                return;
            
            isTraining = true;
            trainingStartTime = DateTime.UtcNow;
            currentSessionIndex = 0;
            totalAgentSteps = 0;
            totalIterations = 0;
        }
        
        // Start training process asynchronously
        Task.Run(async () => await RunTrainingLoop(), cancellationTokenSource.Token);
    }
    
    /// <summary>
    /// Stop the training process
    /// </summary>
    public void StopTraining()
    {
        lock (stateLock)
        {
            isTraining = false;
        }
        
        cancellationTokenSource.Cancel();
        threadPool.Stop();
    }
    
    /// <summary>
    /// Thread-safe method to query current training progress
    /// Called from Unity main thread
    /// </summary>
    public TrainingProgress QueryProgress()
    {
        lock (stateLock)
        {
            return currentProgress;
        }
    }
    
    /// <summary>
    /// Get current agent states for visualization
    /// Thread-safe for Unity main thread access
    /// </summary>
    public List<AgentVisualizationData> GetAgentStatesForVisualization()
    {
        return activeAgentVisualizations.Values.ToList();
    }
    
    /// <summary>
    /// Get completed training results
    /// </summary>
    public List<TrainingResult> GetCompletedResults()
    {
        var results = new List<TrainingResult>();
        while (completedResults.TryDequeue(out TrainingResult result))
        {
            results.Add(result);
        }
        return results;
    }
    
    /// <summary>
    /// Set training speed multiplier
    /// </summary>
    public void SetTimeMultiplier(float multiplier)
    {
        lock (stateLock)
        {
            // Update configuration with new time multiplier
            config.TimeMultiplier = Math.Max(0.1f, Math.Min(10f, multiplier));
        }
    }
    
    /// <summary>
    /// Check if training is currently active
    /// </summary>
    public bool IsTraining => isTraining;
    
    #region Private Methods
    
    private void InitializeTrainingSessionThreadeds()
    {
        TrainingSessionThreadeds = new List<TrainingSessionThreaded>();
        
        // Create training sessions for every 3 consecutive checkpoints
        int totalCheckpoints = checkpointSystem.TotalCheckpoints;
        var raceline = trackMesh.Raceline;
        
        for (int i = 0; i < totalCheckpoints - 2; i++)
        {
            int startCheckpoint = i;
            int goalCheckpoint = (i + 1) % totalCheckpoints;
            int validateCheckpoint = (i + 2) % totalCheckpoints;
            
            // Get starting position and direction for this session
            Vector2D startPos = GetPositionForCheckpoint(startCheckpoint);
            Vector2D startDir = GetDirectionForCheckpoint(startCheckpoint);
            
            var session = new TrainingSessionThreaded(i, startCheckpoint, goalCheckpoint, validateCheckpoint, startPos, startDir);
            TrainingSessionThreadeds.Add(session);
        }
    }
    
    private async Task RunTrainingLoop()
    {
        try
        {
            while (isTraining && currentSessionIndex < TrainingSessionThreadeds.Count && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                await RunTrainingSessionThreaded(TrainingSessionThreadeds[currentSessionIndex]);
                currentSessionIndex++;
            }
        }
        catch (OperationCanceledException)
        {
            // Training was cancelled
        }
        finally
        {
            lock (stateLock)
            {
                isTraining = false;
            }
        }
    }
    
    private async Task RunTrainingSessionThreaded(TrainingSessionThreaded session)
    {
        int iterationsCompleted = 0;
        
        while (iterationsCompleted < config.IterationsPerSession && isTraining && !cancellationTokenSource.Token.IsCancellationRequested)
        {
            // Create batch of agents for this iteration
            var agentBatch = CreateAgentBatch(session, config.AgentsPerBatch);
            
            // Submit work to thread pool
            var workItems = CreateWorkItems(agentBatch, session);
            var tasks = workItems.Select(item => threadPool.SubmitWork(item)).ToArray();
            
            // Wait for batch completion
            await Task.WhenAll(tasks);
            
            // Process results
            ProcessBatchResults(tasks.Select(t => t.Result).ToList(), session);
            
            iterationsCompleted++;
            totalIterations++;
            
            // Update progress
            UpdateProgress(session, iterationsCompleted);
            
            // Small delay to prevent CPU overload
            await Task.Delay((int)(config.IterationDelayMs / config.TimeMultiplier), cancellationTokenSource.Token);
        }
    }
    
    private List<LightweightAgent> CreateAgentBatch(TrainingSessionThreaded session, int batchSize)
    {
        var agents = new List<LightweightAgent>();
        
        for (int i = 0; i < batchSize; i++)
        {
            int agentId = (int)totalIterations * batchSize + i;
            var agent = AgentFactory.CreateTrainingAgent(agentId, session.startPosition, session.startDirection, 
                                                        MotorcycleConfig.Default, config.ExplorationRate);
            agents.Add(agent);
        }
        
        return agents;
    }
    
    private List<AgentWorkItem> CreateWorkItems(List<LightweightAgent> agents, TrainingSessionThreaded session)
    {
        var workItems = new List<AgentWorkItem>();
        
        // Distribute agents across work items for parallel processing
        int agentsPerWorkItem = Math.Max(1, agents.Count / config.ThreadCount);
        
        for (int i = 0; i < agents.Count; i += agentsPerWorkItem)
        {
            var agentSlice = agents.Skip(i).Take(agentsPerWorkItem).ToList();
            var workItem = new AgentWorkItem(agentSlice, session, trackMesh, checkpointSystem, config);
            workItems.Add(workItem);
        }
        
        return workItems;
    }
    
    private void ProcessBatchResults(List<AgentSimulationResult> results, TrainingSessionThreaded session)
    {
        foreach (var result in results)
        {
            // Update best result for session if applicable
            if (result.IsValid && result.CompletionTime < session.bestTime)
            {
                session.bestTime = result.CompletionTime;
                session.bestInputSequence = result.InputSequence;
            }
            
            // Queue result for external consumption
            completedResults.Enqueue(new TrainingResult(result, session.sessionId));
            
            // Update visualization data
            foreach (var agentData in result.AgentVisualizationData)
            {
                activeAgentVisualizations.AddOrUpdate(agentData.agentId, agentData, (key, oldValue) => agentData);
            }
            
            // Update step counter
            Interlocked.Add(ref totalAgentSteps, result.TotalSteps);
        }
    }
    
    private void UpdateProgress(TrainingSessionThreaded session, int iterationsCompleted)
    {
        lock (stateLock)
        {
            var elapsed = DateTime.UtcNow - trainingStartTime;
            
            currentProgress = new TrainingProgress
            {
                currentSession = currentSessionIndex,
                totalSessions = TrainingSessionThreadeds.Count,
                currentIteration = iterationsCompleted,
                totalIterations = config.IterationsPerSession,
                activeAgents = activeAgentVisualizations.Count,
                bestTimeThisSession = session.bestTime,
                isTraining = isTraining,
                trainingStartTime = trainingStartTime.Ticks,
                elapsedTrainingTime = elapsed.TotalSeconds,
                totalAgentSteps = (int)totalAgentSteps,
                agentStepsPerSecond = (float)(totalAgentSteps / Math.Max(elapsed.TotalSeconds, 0.001))
            };
        }
    }
    
    private Vector2D GetPositionForCheckpoint(int checkpointId)
    {
        return checkpointSystem.GetCheckpointPosition(checkpointId);
    }
    
    private Vector2D GetDirectionForCheckpoint(int checkpointId)
    {
        // Get direction by looking toward next checkpoint
        int nextCheckpoint = (checkpointId + 1) % checkpointSystem.TotalCheckpoints;
        Vector2D currentPos = checkpointSystem.GetCheckpointPosition(checkpointId);
        Vector2D nextPos = checkpointSystem.GetCheckpointPosition(nextCheckpoint);
        return (nextPos - currentPos).Normalized;
    }
    
    #endregion
    
    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        StopTraining();
        cancellationTokenSource?.Dispose();
        threadPool?.Dispose();
    }
}

/// <summary>
/// Configuration for training parameters
/// </summary>
[System.Serializable]
public class TrainingConfiguration
{
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public int AgentsPerBatch { get; set; } = 50;
    public int IterationsPerSession { get; set; } = 1000;
    public float TimeMultiplier { get; set; } = 1.0f;
    public float ExplorationRate { get; set; } = 0.1f;
    public int IterationDelayMs { get; set; } = 10;
    public float MaxSimulationTime { get; set; } = 30.0f; // Max time per agent before reset
    public float OffTrackThreshold { get; set; } = 0.25f;
    
    public static TrainingConfiguration Default => new TrainingConfiguration
    {
        ThreadCount = Math.Max(1, Environment.ProcessorCount - 1), // Leave one core for Unity
        AgentsPerBatch = 25,
        IterationsPerSession = 500,
        TimeMultiplier = 1.0f,
        ExplorationRate = 0.15f,
        IterationDelayMs = 5,
        MaxSimulationTime = 20.0f,
        OffTrackThreshold = 0.3f
    };
}

/// <summary>
/// Result data from training operations
/// </summary>
[System.Serializable]
public struct TrainingResult
{
    public int sessionId;
    public int agentId;
    public bool isValid;
    public float completionTime;
    public int checkpointsCompleted;
    public float totalDistance;
    public List<AgentInput> inputSequence;
    public AgentPerformanceMetrics performanceMetrics;
    
    public TrainingResult(AgentSimulationResult simulationResult, int sessionId)
    {
        this.sessionId = sessionId;
        this.agentId = simulationResult.AgentId;
        this.isValid = simulationResult.IsValid;
        this.completionTime = simulationResult.CompletionTime;
        this.checkpointsCompleted = simulationResult.CheckpointsCompleted;
        this.totalDistance = simulationResult.TotalDistance;
        this.inputSequence = simulationResult.InputSequence;
        this.performanceMetrics = simulationResult.PerformanceMetrics;
    }
}