using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unity interface for the multithreaded training system
/// Bridges the thread-safe training engine with Unity's main thread
/// Handles visualization updates and user interface integration
/// </summary>
public class TrainingEngineInterface : MonoBehaviour
{
    [Header("Training Configuration")]
    [SerializeField] private int threadCount = 4;
    [SerializeField] private int agentsPerBatch = 25;
    [SerializeField] private int iterationsPerSession = 500;
    [SerializeField] private float explorationRate = 0.15f;
    [SerializeField] private float trainingSpeedMultiplier = 1.0f;
    
    [Header("Visualization")]
    [SerializeField] private GameObject agentVisualizationPrefab;
    [SerializeField] private Transform agentContainer;
    [SerializeField] private bool showAgentTrails = false;
    [SerializeField] private int maxVisualizedAgents = 50;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showPerformanceMetrics = true;
    
    // Training system components
    private MultiThreadedTrainer trainer;
    private TrackMeshSystem trackMesh;
    private DistanceCheckpointSystem checkpointSystem;
    private TrainingVisualizationManager visualizationManager;
    
    // Unity integration state
    private bool isInitialized = false;
    private TrainingProgress lastProgress;
    private float lastUpdateTime;
    
    // Events for UI system
    public System.Action<TrainingProgress> OnProgressUpdated;
    public System.Action<List<TrainingResult>> OnResultsUpdated;
    public System.Action<bool> OnTrainingStateChanged;
    
    void Start()
    {
        // Wait for track to be loaded before initializing
        TrackMaster.OnTrackLoaded += InitializeTrainingSystem;
        
        if (TrackMaster.GetCurrentRaceline() != null)
        {
            InitializeTrainingSystem();
        }
    }
    
    void Update()
    {
        if (!isInitialized || trainer == null)
            return;
        
        // Update at reasonable frequency (not every frame)
        if (Time.time - lastUpdateTime > 0.1f) // 10 FPS updates
        {
            UpdateVisualization();
            UpdateProgressTracking();
            lastUpdateTime = Time.time;
        }
        
        // Handle keyboard shortcuts
        HandleKeyboardInput();
    }
    
    void OnDestroy()
    {
        StopTraining();
        trainer?.Dispose();
    }
    
    #region Public Interface Methods
    
    /// <summary>
    /// Start the training process
    /// </summary>
    public void StartTraining()
    {
        if (!isInitialized)
        {
            Debug.LogError("Training system not initialized. Ensure track is loaded first.");
            return;
        }
        
        if (trainer.IsTraining)
        {
            Debug.LogWarning("Training is already running.");
            return;
        }
        
        trainer.StartTraining();
        OnTrainingStateChanged?.Invoke(true);
        
        if (enableDebugLogs)
            Debug.Log("Training started with " + threadCount + " threads");
    }
    
    /// <summary>
    /// Stop the training process
    /// </summary>
    public void StopTraining()
    {
        if (trainer != null && trainer.IsTraining)
        {
            trainer.StopTraining();
            OnTrainingStateChanged?.Invoke(false);
            
            if (enableDebugLogs)
                Debug.Log("Training stopped");
        }
    }
    
    /// <summary>
    /// Set training speed multiplier
    /// </summary>
    public void SetTrainingSpeed(float multiplier)
    {
        trainingSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f);
        trainer?.SetTimeMultiplier(trainingSpeedMultiplier);
        
        if (enableDebugLogs)
            Debug.Log($"Training speed set to {trainingSpeedMultiplier}x");
    }
    
    /// <summary>
    /// Get current training progress
    /// </summary>
    public TrainingProgress GetProgress()
    {
        return trainer?.QueryProgress() ?? new TrainingProgress();
    }
    
    /// <summary>
    /// Check if training system is initialized
    /// </summary>
    public bool IsInitialized => isInitialized;
    
    /// <summary>
    /// Check if training is currently running
    /// </summary>
    public bool IsTraining => trainer?.IsTraining ?? false;
    
    #endregion
    
    #region Private Methods
    
    private void InitializeTrainingSystem()
    {
        try
        {
            // Get track data from TrackMaster
            var raceline = TrackMaster.GetCurrentRaceline();
            if (raceline == null || raceline.Count == 0)
            {
                Debug.LogError("No raceline data available for training initialization");
                return;
            }
            
            // Convert Unity Vector2 to Vector2D
            var racelinePoints = raceline.Select(p => new Vector2D(p.x, p.y)).ToList();
            
            // Create dummy boundary data (in a real implementation, this would come from track processing)
            var innerBoundary = CreateDummyBoundary(racelinePoints, -5f);
            var outerBoundary = CreateDummyBoundary(racelinePoints, 5f);
            
            // Initialize core systems
            trackMesh = new TrackMeshSystem(innerBoundary, outerBoundary, racelinePoints);
            checkpointSystem = new DistanceCheckpointSystem(racelinePoints, trackMesh);
            
            // Create training configuration
            var config = new TrainingConfiguration
            {
                ThreadCount = threadCount,
                AgentsPerBatch = agentsPerBatch,
                IterationsPerSession = iterationsPerSession,
                ExplorationRate = explorationRate,
                TimeMultiplier = trainingSpeedMultiplier
            };
            
            // Initialize trainer
            trainer = new MultiThreadedTrainer(config, trackMesh, checkpointSystem);
            
            // Initialize visualization
            visualizationManager = new TrainingVisualizationManager(agentVisualizationPrefab, agentContainer, maxVisualizedAgents);
            
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log($"Training system initialized with {racelinePoints.Count} raceline points and {checkpointSystem.TotalCheckpoints} checkpoints");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize training system: {ex.Message}");
        }
    }
    
    private void UpdateVisualization()
    {
        if (!isInitialized || visualizationManager == null)
            return;
        
        // Get current agent states
        var agentStates = trainer.GetAgentStatesForVisualization();
        
        // Update visualization
        visualizationManager.UpdateAgentVisualizations(agentStates);
        
        // Update checkpoint visualization if needed
        UpdateCheckpointVisualization();
    }
    
    private void UpdateProgressTracking()
    {
        var currentProgress = trainer.QueryProgress();
        
        // Check if progress has changed significantly
        if (HasProgressChanged(currentProgress))
        {
            lastProgress = currentProgress;
            OnProgressUpdated?.Invoke(currentProgress);
            
            // Log performance metrics periodically
            if (showPerformanceMetrics && enableDebugLogs && currentProgress.currentIteration % 100 == 0)
            {
                LogPerformanceMetrics(currentProgress);
            }
        }
        
        // Check for new results
        var newResults = trainer.GetCompletedResults();
        if (newResults.Count > 0)
        {
            OnResultsUpdated?.Invoke(newResults);
            
            if (enableDebugLogs)
                Debug.Log($"Received {newResults.Count} new training results");
        }
    }
    
    private void UpdateCheckpointVisualization()
    {
        // Update checkpoint visual states based on training progress
        // This would integrate with the existing CheckpointManager if needed
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (trainer.IsTraining)
                StopTraining();
            else
                StartTraining();
        }
        
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
        {
            SetTrainingSpeed(trainingSpeedMultiplier * 1.5f);
        }
        
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            SetTrainingSpeed(trainingSpeedMultiplier / 1.5f);
        }
    }
    
    private bool HasProgressChanged(TrainingProgress current)
    {
        return current.currentIteration != lastProgress.currentIteration ||
               current.currentSession != lastProgress.currentSession ||
               current.activeAgents != lastProgress.activeAgents;
    }
    
    private void LogPerformanceMetrics(TrainingProgress progress)
    {
        Debug.Log($"Training Progress - Session: {progress.currentSession}/{progress.totalSessions}, " +
                 $"Iteration: {progress.currentIteration}/{progress.totalIterations}, " +
                 $"Agents/sec: {progress.agentStepsPerSecond:F1}, " +
                 $"Best Time: {progress.bestTimeThisSession:F2}s");
    }
    
    private List<Vector2D> CreateDummyBoundary(List<Vector2D> raceline, float offset)
    {
        // Create a parallel line offset from the raceline
        // This is a simplified implementation - in practice, this would come from track processing
        var boundary = new List<Vector2D>();
        
        for (int i = 0; i < raceline.Count; i++)
        {
            var current = raceline[i];
            var next = raceline[(i + 1) % raceline.Count];
            var direction = (next - current).Normalized;
            var perpendicular = Vector2D.Perpendicular(direction);
            
            boundary.Add(current + perpendicular * offset);
        }
        
        return boundary;
    }
    
    #endregion
}