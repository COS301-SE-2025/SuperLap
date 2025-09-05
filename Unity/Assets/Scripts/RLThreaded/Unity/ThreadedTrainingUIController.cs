using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Unity UI controller for the multithreaded training system
/// Provides user interface for training control and progress monitoring
/// </summary>
public class ThreadedTrainingUIController : MonoBehaviour
{
    [Header("Main Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;
    
    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI sessionText;
    [SerializeField] private TextMeshProUGUI iterationText;
    [SerializeField] private TextMeshProUGUI activeAgentsText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private Slider progressSlider;
    
    [Header("Performance Metrics")]
    [SerializeField] private TextMeshProUGUI agentStepsPerSecText;
    [SerializeField] private TextMeshProUGUI totalStepsText;
    [SerializeField] private TextMeshProUGUI elapsedTimeText;
    [SerializeField] private TextMeshProUGUI trainingStatusText;
    
    [Header("Configuration")]
    [SerializeField] private Slider threadCountSlider;
    [SerializeField] private TextMeshProUGUI threadCountText;
    [SerializeField] private Slider agentsPerBatchSlider;
    [SerializeField] private TextMeshProUGUI agentsPerBatchText;
    [SerializeField] private Slider iterationsSlider;
    [SerializeField] private TextMeshProUGUI iterationsText;
    [SerializeField] private Slider explorationSlider;
    [SerializeField] private TextMeshProUGUI explorationText;
    
    [Header("Visualization Controls")]
    [SerializeField] private Toggle showTrailsToggle;
    [SerializeField] private Toggle showPerformanceToggle;
    [SerializeField] private Slider maxAgentsSlider;
    [SerializeField] private TextMeshProUGUI maxAgentsText;
    
    [Header("Results Display")]
    [SerializeField] private ScrollRect resultsScrollRect;
    [SerializeField] private Transform resultsContent;
    [SerializeField] private GameObject resultItemPrefab;
    
    // References
    private TrainingEngineInterface trainingEngine;
    private TrainingVisualizationManager visualizationManager;
    
    // State tracking
    private bool isInitialized = false;
    private List<GameObject> resultItems = new List<GameObject>();
    
    void Start()
    {
        InitializeUI();
        FindTrainingEngine();
    }
    
    void Update()
    {
        if (isInitialized && trainingEngine != null)
        {
            UpdateProgressDisplay();
        }
    }
    
    #region Initialization
    
    private void InitializeUI()
    {
        // Setup button callbacks
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);
        
        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopButtonClicked);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);
        
        // Setup slider callbacks
        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
            speedSlider.value = 1.0f;
            OnSpeedSliderChanged(1.0f);
        }
        
        if (threadCountSlider != null)
        {
            threadCountSlider.onValueChanged.AddListener(OnThreadCountChanged);
            threadCountSlider.value = System.Environment.ProcessorCount - 1;
            OnThreadCountChanged(threadCountSlider.value);
        }
        
        if (agentsPerBatchSlider != null)
        {
            agentsPerBatchSlider.onValueChanged.AddListener(OnAgentsPerBatchChanged);
            agentsPerBatchSlider.value = 25;
            OnAgentsPerBatchChanged(25);
        }
        
        if (iterationsSlider != null)
        {
            iterationsSlider.onValueChanged.AddListener(OnIterationsChanged);
            iterationsSlider.value = 500;
            OnIterationsChanged(500);
        }
        
        if (explorationSlider != null)
        {
            explorationSlider.onValueChanged.AddListener(OnExplorationChanged);
            explorationSlider.value = 0.15f;
            OnExplorationChanged(0.15f);
        }
        
        if (maxAgentsSlider != null)
        {
            maxAgentsSlider.onValueChanged.AddListener(OnMaxAgentsChanged);
            maxAgentsSlider.value = 50;
            OnMaxAgentsChanged(50);
        }
        
        // Setup toggle callbacks
        if (showTrailsToggle != null)
            showTrailsToggle.onValueChanged.AddListener(OnShowTrailsChanged);
        
        if (showPerformanceToggle != null)
            showPerformanceToggle.onValueChanged.AddListener(OnShowPerformanceChanged);
        
        // Initial UI state
        UpdateButtonStates(false);
        
        isInitialized = true;
    }
    
    private void FindTrainingEngine()
    {
        trainingEngine = FindAnyObjectByType<TrainingEngineInterface>();
        
        if (trainingEngine != null)
        {
            // Subscribe to events
            trainingEngine.OnProgressUpdated += OnProgressUpdated;
            trainingEngine.OnResultsUpdated += OnResultsUpdated;
            trainingEngine.OnTrainingStateChanged += OnTrainingStateChanged;
        }
        else
        {
            Debug.LogWarning("TrainingEngineInterface not found. UI will not function properly.");
        }
    }
    
    #endregion
    
    #region Button Callbacks
    
    private void OnStartButtonClicked()
    {
        if (trainingEngine != null)
        {
            trainingEngine.StartTraining();
        }
    }
    
    private void OnStopButtonClicked()
    {
        if (trainingEngine != null)
        {
            trainingEngine.StopTraining();
        }
    }
    
    private void OnResetButtonClicked()
    {
        // Clear results display
        ClearResults();
        
        // Reset visualization
        // This would reset the training system if needed
    }
    
    #endregion
    
    #region Slider Callbacks
    
    private void OnSpeedSliderChanged(float value)
    {
        if (speedText != null)
            speedText.text = $"{value:F1}x";
        
        if (trainingEngine != null)
            trainingEngine.SetTrainingSpeed(value);
    }
    
    private void OnThreadCountChanged(float value)
    {
        int threadCount = Mathf.RoundToInt(value);
        if (threadCountText != null)
            threadCountText.text = threadCount.ToString();
    }
    
    private void OnAgentsPerBatchChanged(float value)
    {
        int agentCount = Mathf.RoundToInt(value);
        if (agentsPerBatchText != null)
            agentsPerBatchText.text = agentCount.ToString();
    }
    
    private void OnIterationsChanged(float value)
    {
        int iterations = Mathf.RoundToInt(value);
        if (iterationsText != null)
            iterationsText.text = iterations.ToString();
    }
    
    private void OnExplorationChanged(float value)
    {
        if (explorationText != null)
            explorationText.text = $"{value:F2}";
    }
    
    private void OnMaxAgentsChanged(float value)
    {
        int maxAgents = Mathf.RoundToInt(value);
        if (maxAgentsText != null)
            maxAgentsText.text = maxAgents.ToString();
    }
    
    #endregion
    
    #region Toggle Callbacks
    
    private void OnShowTrailsChanged(bool value)
    {
        // Update visualization manager if available
        if (visualizationManager != null)
        {
            visualizationManager.SetShowTrails(value);
        }
    }
    
    private void OnShowPerformanceChanged(bool value)
    {
        if (visualizationManager != null)
        {
            visualizationManager.SetShowPerformanceIndicators(value);
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnProgressUpdated(TrainingProgress progress)
    {
        // Progress will be updated in the Update method
    }
    
    private void OnResultsUpdated(List<TrainingResult> results)
    {
        foreach (var result in results)
        {
            AddResultItem(result);
        }
    }
    
    private void OnTrainingStateChanged(bool isTraining)
    {
        UpdateButtonStates(isTraining);
        
        if (trainingStatusText != null)
        {
            trainingStatusText.text = isTraining ? "Training..." : "Stopped";
            trainingStatusText.color = isTraining ? Color.green : Color.red;
        }
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateProgressDisplay()
    {
        if (trainingEngine == null)
            return;
        
        var progress = trainingEngine.GetProgress();
        
        // Update session info
        if (sessionText != null)
            sessionText.text = $"Session: {progress.currentSession + 1}/{progress.totalSessions}";
        
        if (iterationText != null)
            iterationText.text = $"Iteration: {progress.currentIteration}/{progress.totalIterations}";
        
        if (activeAgentsText != null)
            activeAgentsText.text = $"Active Agents: {progress.activeAgents}";
        
        if (bestTimeText != null)
        {
            string timeText = progress.bestTimeThisSession < float.MaxValue ? 
                $"{progress.bestTimeThisSession:F2}s" : "None";
            bestTimeText.text = $"Best Time: {timeText}";
        }
        
        // Update progress bar
        if (progressSlider != null)
        {
            float sessionProgress = progress.totalSessions > 0 ? 
                (float)progress.currentSession / progress.totalSessions : 0f;
            float iterationProgress = progress.totalIterations > 0 ? 
                (float)progress.currentIteration / progress.totalIterations : 0f;
            
            // Combine session and iteration progress
            float totalProgress = (sessionProgress + iterationProgress / progress.totalSessions);
            progressSlider.value = totalProgress;
        }
        
        // Update performance metrics
        if (agentStepsPerSecText != null)
            agentStepsPerSecText.text = $"Steps/sec: {progress.agentStepsPerSecond:F1}";
        
        if (totalStepsText != null)
            totalStepsText.text = $"Total Steps: {progress.totalAgentSteps:N0}";
        
        if (elapsedTimeText != null)
        {
            System.TimeSpan elapsed = System.TimeSpan.FromSeconds(progress.elapsedTrainingTime);
            elapsedTimeText.text = $"Elapsed: {elapsed:hh\\:mm\\:ss}";
        }
    }
    
    private void UpdateButtonStates(bool isTraining)
    {
        if (startButton != null)
            startButton.interactable = !isTraining;
        
        if (stopButton != null)
            stopButton.interactable = isTraining;
        
        // Disable configuration controls during training
        if (threadCountSlider != null)
            threadCountSlider.interactable = !isTraining;
        
        if (agentsPerBatchSlider != null)
            agentsPerBatchSlider.interactable = !isTraining;
        
        if (iterationsSlider != null)
            iterationsSlider.interactable = !isTraining;
    }
    
    private void AddResultItem(TrainingResult result)
    {
        if (resultItemPrefab == null || resultsContent == null)
            return;
        
        GameObject item = Instantiate(resultItemPrefab, resultsContent);
        
        // Configure result item (assuming it has text components)
        var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 3)
        {
            texts[0].text = $"Agent {result.agentId}";
            texts[1].text = result.isValid ? $"{result.completionTime:F2}s" : "Failed";
            texts[2].text = $"{result.checkpointsCompleted} checkpoints";
        }
        
        resultItems.Add(item);
        
        // Scroll to bottom
        if (resultsScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            resultsScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    private void ClearResults()
    {
        foreach (var item in resultItems)
        {
            if (item != null)
                DestroyImmediate(item);
        }
        resultItems.Clear();
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (trainingEngine != null)
        {
            trainingEngine.OnProgressUpdated -= OnProgressUpdated;
            trainingEngine.OnResultsUpdated -= OnResultsUpdated;
            trainingEngine.OnTrainingStateChanged -= OnTrainingStateChanged;
        }
    }
}