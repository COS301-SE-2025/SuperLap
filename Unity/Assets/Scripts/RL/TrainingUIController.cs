using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class TrainingUIController : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Button to start/stop training")]
    [SerializeField] private Button startStopButton;
    
    [Tooltip("Button to reset training")]
    [SerializeField] private Button resetButton;
    
    [Tooltip("Button to save best training data")]
    [SerializeField] private Button saveButton;
    
    [Tooltip("Input field for agent count")]
    [SerializeField] private TMP_InputField agentCountInput;
    
    [Tooltip("Input field for iteration count")]
    [SerializeField] private TMP_InputField iterationCountInput;
    
    [Tooltip("Text displaying current training progress")]
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Tooltip("Text on the start/stop button")]
    [SerializeField] private TextMeshProUGUI startStopButtonText;
    
    [Header("Training References")]
    [Tooltip("Reference to the Trainer component")]
    [SerializeField] private Trainer trainer;
    
    [Header("UI Settings")]
    [Tooltip("Update frequency for progress display (in seconds)")]
    [SerializeField] private float progressUpdateInterval = 0.5f;
    
    [Tooltip("File path for saving training data")]
    [SerializeField] private string saveFilePath = "Assets/TrainingData/";
    
    // Internal state
    private float lastProgressUpdate = 0f;
    private bool isTrainingActive = false;
    
    void Start()
    {
        InitializeUI();
        SetupEventListeners();
        
        // Find trainer if not assigned
        if (trainer == null)
        {
            trainer = FindAnyObjectByType<Trainer>();
            if (trainer == null)
            {
                Debug.LogError("TrainingUIController: No Trainer found in scene!");
            }
        }
        
        Debug.Log("Training UI Controller initialized");
    }
    
    void InitializeUI()
    {
        // Set default values
        if (agentCountInput != null)
        {
            agentCountInput.text = "4";
        }
        
        if (iterationCountInput != null)
        {
            iterationCountInput.text = "100";
        }
        
        if (progressText != null)
        {
            progressText.text = "Training Progress: 0%";
        }
        
        if (startStopButtonText != null)
        {
            startStopButtonText.text = "Start Training";
        }
        
        // Ensure save directory exists
        if (!Directory.Exists(saveFilePath))
        {
            Directory.CreateDirectory(saveFilePath);
        }
    }
    
    void SetupEventListeners()
    {
        if (startStopButton != null)
        {
            startStopButton.onClick.AddListener(OnStartStopClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
        
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
        }
        
        if (agentCountInput != null)
        {
            agentCountInput.onEndEdit.AddListener(OnAgentCountChanged);
        }
        
        if (iterationCountInput != null)
        {
            iterationCountInput.onEndEdit.AddListener(OnIterationCountChanged);
        }
    }
    
    void Update()
    {
        // Update progress display
        if (Time.time - lastProgressUpdate >= progressUpdateInterval)
        {
            UpdateProgressDisplay();
            lastProgressUpdate = Time.time;
        }
        
        // Update UI state based on trainer
        UpdateUIState();
    }
    
    void UpdateProgressDisplay()
    {
        if (trainer == null || progressText == null) return;
        
        float progressPercentage = CalculateTrainingProgress();
        string progressString = $"Training Progress: {progressPercentage:F1}%";
        
        // Add current session info if training is active
        if (trainer.IsTraining)
        {
            var sessions = trainer.TrainingSessions;
            if (sessions != null && trainer.CurrentSessionIndex < sessions.Count)
            {
                var currentSession = sessions[trainer.CurrentSessionIndex];
                progressString += $"\nSession {trainer.CurrentSessionIndex + 1}/{sessions.Count}";
                progressString += $"\nIteration {trainer.CurrentIteration}/{trainer.Iterations}";
                progressString += $"\nActive Agents: {trainer.ActiveAgents}";
                progressString += $"\nCheckpoints: {currentSession.startCheckpoint}→{currentSession.goalCheckpoint}→{currentSession.validateCheckpoint}";
                
                if (currentSession.bestTime < float.MaxValue)
                {
                    progressString += $"\nBest Time: {currentSession.bestTime:F2}s";
                }
            }
        }
        
        progressText.text = progressString;
    }
    
    void UpdateUIState()
    {
        if (trainer == null) return;
        
        bool trainingActive = trainer.IsTraining;
        
        // Update button text
        if (startStopButtonText != null)
        {
            startStopButtonText.text = trainingActive ? "Stop Training" : "Start Training";
        }
        
        // Enable/disable input fields during training
        if (agentCountInput != null)
        {
            agentCountInput.interactable = !trainingActive;
        }
        
        if (iterationCountInput != null)
        {
            iterationCountInput.interactable = !trainingActive;
        }
        
        // Enable save button only if we have training data
        if (saveButton != null)
        {
            bool hasData = HasTrainingData();
            saveButton.interactable = hasData && !trainingActive;
        }
        
        isTrainingActive = trainingActive;
    }
    
    float CalculateTrainingProgress()
    {
        if (trainer == null) return 0f;
        
        var sessions = trainer.TrainingSessions;
        if (sessions == null || sessions.Count == 0) return 0f;
        
        int totalSessions = sessions.Count;
        int currentSessionIndex = trainer.CurrentSessionIndex;
        
        if (!trainer.IsTraining)
        {
            // If not training, check how many sessions are complete
            int completedSessions = 0;
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].isComplete)
                {
                    completedSessions++;
                }
            }
            return (float)completedSessions / totalSessions * 100f;
        }
        
        // If training is active, calculate progress within current session
        if (currentSessionIndex >= totalSessions) return 100f;
        
        // Calculate session progress (how many sessions completed)
        float sessionProgress = (float)currentSessionIndex / totalSessions;
        
        // Add progress within current session based on iterations
        float withinSessionProgress = 0f;
        if (trainer.Iterations > 0)
        {
            withinSessionProgress = (float)trainer.CurrentIteration / trainer.Iterations;
            withinSessionProgress = Mathf.Clamp01(withinSessionProgress);
        }
        
        // Weight the within-session progress
        float currentSessionWeight = 1f / totalSessions;
        sessionProgress += withinSessionProgress * currentSessionWeight;
        
        return Mathf.Clamp01(sessionProgress) * 100f;
    }
    
    bool HasTrainingData()
    {
        if (trainer == null) return false;
        
        var sessions = trainer.TrainingSessions;
        if (sessions == null) return false;
        
        foreach (var session in sessions)
        {
            if (session.bestState != null && session.bestState.isValid)
            {
                return true;
            }
        }
        
        return false;
    }
    
    #region Button Event Handlers
    
    public void OnStartStopClicked()
    {
        if (trainer == null)
        {
            Debug.LogError("No trainer reference found!");
            return;
        }
        
        if (trainer.IsTraining)
        {
            // Stop training
            StopTraining();
        }
        else
        {
            // Start training
            StartTraining();
        }
    }
    
    public void OnResetClicked()
    {
        if (trainer == null) return;
        
        // Reset training using the new public method
        trainer.ResetTraining();
        
        Debug.Log("Training reset");
        
        // Update progress display
        UpdateProgressDisplay();
    }
    
    public void OnSaveClicked()
    {
        if (trainer == null || !HasTrainingData())
        {
            Debug.LogWarning("No training data to save!");
            return;
        }
        
        SaveTrainingData();
    }
    
    public void OnAgentCountChanged(string value)
    {
        if (int.TryParse(value, out int agentCount))
        {
            agentCount = Mathf.Clamp(agentCount, 1, 10); // Reasonable limits
            
            if (agentCountInput != null)
            {
                agentCountInput.text = agentCount.ToString();
            }
            
            // Update trainer if not currently training
            if (trainer != null && !trainer.IsTraining)
            {
                trainer.SetAgentCount(agentCount);
                Debug.Log($"Agent count updated to: {agentCount}");
            }
        }
    }
    
    public void OnIterationCountChanged(string value)
    {
        if (int.TryParse(value, out int iterationCount))
        {
            iterationCount = Mathf.Max(1, iterationCount); // At least 1
            
            if (iterationCountInput != null)
            {
                iterationCountInput.text = iterationCount.ToString();
            }
            
            // Update trainer if not currently training
            if (trainer != null && !trainer.IsTraining)
            {
                trainer.SetIterations(iterationCount);
                Debug.Log($"Iteration count updated to: {iterationCount}");
            }
        }
    }
    
    #endregion
    
    #region Training Control
    
    void StartTraining()
    {
        if (trainer == null) return;
        
        // Update trainer parameters before starting
        OnAgentCountChanged(agentCountInput?.text ?? "4");
        OnIterationCountChanged(iterationCountInput?.text ?? "100");
        
        // Start training using the new public method
        trainer.StartTraining();
    }
    
    void StopTraining()
    {
        if (trainer == null) return;
        
        // Stop training using the new public method
        trainer.StopTraining();
    }
    
    #endregion
    
    #region Data Saving
    
    void SaveTrainingData()
    {
        if (trainer == null) return;
        
        var sessions = trainer.TrainingSessions;
        if (sessions == null) return;
        
        StringBuilder dataBuilder = new StringBuilder();
        dataBuilder.AppendLine("SuperLap Training Data Export");
        dataBuilder.AppendLine($"Export Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        dataBuilder.AppendLine($"Total Sessions: {sessions.Count}");
        dataBuilder.AppendLine();
        
        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            dataBuilder.AppendLine($"=== SESSION {i + 1} ===");
            dataBuilder.AppendLine($"Start Checkpoint: {session.startCheckpoint}");
            dataBuilder.AppendLine($"Goal Checkpoint: {session.goalCheckpoint}");
            dataBuilder.AppendLine($"Validate Checkpoint: {session.validateCheckpoint}");
            dataBuilder.AppendLine($"Best Time: {session.bestTime:F3}s");
            dataBuilder.AppendLine($"Completed: {session.isComplete}");
            dataBuilder.AppendLine($"Valid Data: {session.bestState?.isValid ?? false}");
            
            if (session.bestState != null && session.bestState.isValid)
            {
                var state = session.bestState;
                dataBuilder.AppendLine($"Best Position: {state.position}");
                dataBuilder.AppendLine($"Best Direction: {state.forward}");
                dataBuilder.AppendLine($"Best Speed: {state.speed:F2} m/s");
                dataBuilder.AppendLine($"Best Turn Angle: {state.turnAngle:F2}°");
                
                if (state.inputSequence != null && state.inputSequence.Count > 0)
                {
                    dataBuilder.AppendLine("Input Sequence:");
                    for (int j = 0; j < state.inputSequence.Count; j++)
                    {
                        var input = state.inputSequence[j];
                        dataBuilder.AppendLine($"  Step {j}: Speed={input.Item1}, Turn={input.Item2}");
                    }
                }
            }
            
            dataBuilder.AppendLine();
        }
        
        // Save to file
        string fileName = $"training_data_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string fullPath = Path.Combine(saveFilePath, fileName);
        
        try
        {
            File.WriteAllText(fullPath, dataBuilder.ToString());
            Debug.Log($"Training data saved to: {fullPath}");
            
            // Show success message (you could add a UI popup here)
            if (progressText != null)
            {
                string originalText = progressText.text;
                progressText.text = "Data saved successfully!";
                
                // Restore original text after 2 seconds
                Invoke(nameof(RestoreProgressText), 2f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save training data: {e.Message}");
        }
    }
    
    void RestoreProgressText()
    {
        UpdateProgressDisplay();
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (startStopButton != null)
        {
            startStopButton.onClick.RemoveListener(OnStartStopClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnResetClicked);
        }
        
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveClicked);
        }
        
        if (agentCountInput != null)
        {
            agentCountInput.onEndEdit.RemoveListener(OnAgentCountChanged);
        }
        
        if (iterationCountInput != null)
        {
            iterationCountInput.onEndEdit.RemoveListener(OnIterationCountChanged);
        }
    }
}
