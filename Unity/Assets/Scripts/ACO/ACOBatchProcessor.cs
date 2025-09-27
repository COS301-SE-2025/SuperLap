using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RacelineOptimizer;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;

public class ACOBatchProcessor : MonoBehaviour
{
    [Header("Batch Processing Settings")]
    [SerializeField] private string inputDirectory = "Input";
    [SerializeField] private bool processOnStart = false;
    [SerializeField] private bool useAbsolutePath = false;
    
    [Header("Processing Settings")]
    [SerializeField] private float delayBetweenFiles = 2.0f;
    [SerializeField] private int maxConcurrentProcessing = 1;
    
    [Header("ACO Training Settings")]
    [SerializeField] private int agentCount = 100;
    [SerializeField] private int threadCount = 1;
    [SerializeField] private int checkpointCount = 10;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnBatchStarted;
    public UnityEngine.Events.UnityEvent OnBatchCompleted;
    public UnityEngine.Events.UnityEvent<string> OnFileProcessed;
    public UnityEngine.Events.UnityEvent<string> OnProcessingError;
    
    private Queue<string> filesToProcess = new Queue<string>();
    private bool isProcessing = false;
    private int processedCount = 0;
    private int totalFiles = 0;
    private string currentProcessingFile = "";
    
    // References to required components
    private ACOTrainer acoTrainer;
    
    void Start()
    {
        // Get or add required components
        acoTrainer = GetComponent<ACOTrainer>();
        if (acoTrainer == null)
        {
            acoTrainer = gameObject.AddComponent<ACOTrainer>();
        }
        
        // Configure ACO trainer
        acoTrainer.SetAgentCount(agentCount);
        acoTrainer.SetThreadCount(threadCount);
        acoTrainer.SetCheckPointCount(checkpointCount);
        
        if (processOnStart)
        {
            StartBatchProcessing();
        }
    }
    
    void Update()
    {
        if (isProcessing && !acoTrainer.IsDone() && filesToProcess.Count > 0)
        {
            // Check if current training is complete
            if (acoTrainer.IsDone())
            {
                FinishCurrentFile();
            }
        }
    }
    
    [ContextMenu("Start Batch Processing")]
    public void StartBatchProcessing()
    {
        if (isProcessing)
        {
            LogDebug("Batch processing already in progress!");
            return;
        }
        
        if (!ValidateConfiguration())
        {
            return;
        }
        
        string directoryPath = useAbsolutePath ? inputDirectory : Path.Combine(Application.streamingAssetsPath, inputDirectory);
        
        // Find all .bin files in the directory
        string[] binFiles = Directory.GetFiles(directoryPath, "*.bin");
        
        if (binFiles.Length == 0)
        {
            LogError($"No .bin files found in directory: {directoryPath}");
            return;
        }
        
        // Queue up all files for processing
        filesToProcess.Clear();
        foreach (string filePath in binFiles)
        {
            filesToProcess.Enqueue(filePath);
        }
        
        totalFiles = binFiles.Length;
        processedCount = 0;
        isProcessing = true;
        
        LogDebug($"Starting batch processing of {totalFiles} files from: {directoryPath}");
        OnBatchStarted?.Invoke();
        
        // Start processing the first file
        StartCoroutine(ProcessNextFile());
    }
    
    private IEnumerator ProcessNextFile()
    {
        yield return new WaitForSeconds(1);
        if (filesToProcess.Count == 0)
        {
            CompleteBatchProcessing();
            yield break;
        }
        
        currentProcessingFile = filesToProcess.Dequeue();
        string fileName = Path.GetFileNameWithoutExtension(currentProcessingFile);
        
        LogDebug($"Processing file {processedCount + 1}/{totalFiles}: {fileName}");
        
        // Load the track data from the .bin file
        EdgeData edgeData = EdgeData.LoadFromBinary(currentProcessingFile, true);
        
        if (edgeData == null || edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
        {
            LogError($"Failed to load track data from: {currentProcessingFile}");
            OnProcessingError?.Invoke(currentProcessingFile);
            processedCount++;
            yield return new WaitForSeconds(delayBetweenFiles);
            StartCoroutine(ProcessNextFile());
            yield break;
        }
        
        // Convert System.Numerics.Vector2 to UnityEngine.Vector2 for ACOTrackMaster
        List<Vector2> innerBoundary = edgeData.InnerBoundary.Select(v => new Vector2(v.X, v.Y)).ToList();
        List<Vector2> outerBoundary = edgeData.OuterBoundary.Select(v => new Vector2(v.X, v.Y)).ToList();
        List<Vector2> raceline = edgeData.Raceline.Select(v => new Vector2(v.X, v.Y)).ToList();
        
        // Create TrackImageProcessor.ProcessingResults for ACOTrackMaster
        var processingResults = new TrackImageProcessor.ProcessingResults
        {
            success = true,
            errorMessage = "",
            innerBoundary = innerBoundary,
            outerBoundary = outerBoundary,
            raceline = raceline,
            processingTime = 0f,
            centerlinePoints = new List<Vector2>(),
            startPosition = null,
            raceDirection = 0f
        };
        
        // Load the track into ACOTrackMaster
        ACOTrackMaster.LoadTrack(processingResults);
        
        // Wait a frame to ensure track is loaded
        yield return null;
        
        // Start ACO training
        acoTrainer.StartTraining();
        
        LogDebug($"Started ACO training for: {fileName}");
        
        // Wait for training to complete
        yield return new WaitUntil(() => acoTrainer.IsDone());
        
        // Save the results
        SaveACOResults(currentProcessingFile);
        
        processedCount++;
        OnFileProcessed?.Invoke(currentProcessingFile);
        
        // Wait before processing next file
        yield return new WaitForSeconds(delayBetweenFiles);
        
        // Process next file
        StartCoroutine(ProcessNextFile());
    }
    
    private void SaveACOResults(string originalFilePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(originalFilePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFilePath);
            string outputFileName = $"{fileNameWithoutExt}_ACO.bin";
            string outputPath = Path.Combine(directory, outputFileName);
            
            // Get the ACO agent replay data
            ACOAgentReplay agentReplay = GetComponent<ACOAgentReplay>();
            if (agentReplay == null)
            {
                agentReplay = gameObject.AddComponent<ACOAgentReplay>();
            }
            
            // Show the agent replay to get the replay states
            acoTrainer.ShowAgentReplay();
            
            // Wait a frame for the replay to be initialized
            StartCoroutine(SaveACOResultsDelayed(outputPath, agentReplay));
        }
        catch (System.Exception e)
        {
            LogError($"Failed to save ACO results for {originalFilePath}: {e.Message}");
            OnProcessingError?.Invoke(originalFilePath);
        }
    }
    
    private IEnumerator SaveACOResultsDelayed(string outputPath, ACOAgentReplay agentReplay)
    {
        yield return null; // Wait one frame
        
        try
        {
            // Instead of using the original SaveBinFile, create our own custom save method
            SaveACOResultsToCustomPath(agentReplay, outputPath, 5.0f);
            LogDebug($"Saved ACO results to: {outputPath}");
        }
        catch (System.Exception e)
        {
            LogError($"Failed to save ACO results to {outputPath}: {e.Message}");
            OnProcessingError?.Invoke(outputPath);
        }
    }
    
    private void SaveACOResultsToCustomPath(ACOAgentReplay agentReplay, string outputPath, float simplificationTolerance)
    {
        // Get colored segments first
        var segments = agentReplay.GetColoredSegments();

        // Simplify the colored segments while preserving color boundaries
        var simplifiedSegments = agentReplay.SimplifyColoredSegments(segments, simplificationTolerance);

        // Extract the simplified player line from the simplified segments
        List<Vector2> simplifiedPlayerLine = new List<Vector2>();
        if (simplifiedSegments.Count > 0)
        {
            simplifiedPlayerLine.Add(simplifiedSegments[0].start);
            foreach (var segment in simplifiedSegments)
            {
                simplifiedPlayerLine.Add(segment.end);
            }
        }

        // Group simplified segments by color for output
        List<Vector2> redSegments = new List<Vector2>();
        List<Vector2> yellowSegments = new List<Vector2>();
        List<Vector2> greenSegments = new List<Vector2>();

        foreach (var segment in simplifiedSegments)
        {
            if (segment.color.Equals(Color.red))
            {
                redSegments.Add(segment.start);
                redSegments.Add(segment.end);
            }
            else if (segment.color.Equals(Color.yellow))
            {
                yellowSegments.Add(segment.start);
                yellowSegments.Add(segment.end);
            }
            else if (segment.color.Equals(Color.green))
            {
                greenSegments.Add(segment.start);
                greenSegments.Add(segment.end);
            }
        }

        var trackData = ACOTrackMaster.instance.LastProcessingResults;
        string outputDir = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        using (var writer = new BinaryWriter(File.Create(outputPath)))
        {
            WritePoints(writer, trackData.outerBoundary);
            WritePoints(writer, trackData.innerBoundary);
            WritePoints(writer, simplifiedPlayerLine); // Use simplified line
            WritePoints(writer, yellowSegments);
            WritePoints(writer, redSegments);
        }

        LogDebug($"Simplified track with {simplifiedPlayerLine.Count} points written to: {outputPath}");
    }
    
    private void WritePoints(BinaryWriter writer, List<Vector2> points)
    {
        writer.Write(points.Count);
        foreach (var pt in points)
        {
            writer.Write(pt.x);
            writer.Write(pt.y);
        }
    }
    
    private void FinishCurrentFile()
    {
        LogDebug($"Completed processing: {Path.GetFileNameWithoutExtension(currentProcessingFile)}");
    }
    
    private void CompleteBatchProcessing()
    {
        isProcessing = false;
        LogDebug($"Batch processing completed! Processed {processedCount}/{totalFiles} files.");
        OnBatchCompleted?.Invoke();
        
        // Reset state
        filesToProcess.Clear();
        currentProcessingFile = "";
        processedCount = 0;
        totalFiles = 0;
    }
    
    [ContextMenu("Stop Batch Processing")]
    public void StopBatchProcessing()
    {
        if (!isProcessing)
        {
            LogDebug("No batch processing in progress.");
            return;
        }
        
        StopAllCoroutines();
        isProcessing = false;
        filesToProcess.Clear();
        LogDebug("Batch processing stopped.");
    }
    
    public float GetProgress()
    {
        if (totalFiles == 0) return 0f;
        return (float)processedCount / totalFiles;
    }
    
    public string GetCurrentStatus()
    {
        if (!isProcessing)
        {
            return "Idle";
        }
        
        if (string.IsNullOrEmpty(currentProcessingFile))
        {
            return "Starting...";
        }
        
        string fileName = Path.GetFileNameWithoutExtension(currentProcessingFile);
        return $"Processing {processedCount + 1}/{totalFiles}: {fileName}";
    }
    
    public bool IsProcessing()
    {
        return isProcessing;
    }
    
    // Public methods to configure settings at runtime
    public void SetInputDirectory(string directory)
    {
        if (!isProcessing)
        {
            inputDirectory = directory;
        }
    }
    
    public void SetUseAbsolutePath(bool useAbsolute)
    {
        if (!isProcessing)
        {
            useAbsolutePath = useAbsolute;
        }
    }
    
    public void SetAgentCount(int count)
    {
        agentCount = count;
        if (acoTrainer != null)
        {
            acoTrainer.SetAgentCount(count);
        }
    }
    
    public void SetThreadCount(int count)
    {
        threadCount = count;
        if (acoTrainer != null)
        {
            acoTrainer.SetThreadCount(count);
        }
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ACOBatchProcessor] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ACOBatchProcessor] {message}");
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("ACO Batch Processor", GUI.skin.label);
        GUILayout.Space(5);
        
        GUILayout.Label($"Status: {GetCurrentStatus()}");
        
        if (isProcessing)
        {
            float progress = GetProgress();
            GUILayout.Label($"Progress: {(progress * 100):F1}%");
            
            // Simple progress bar
            Rect progressRect = GUILayoutUtility.GetRect(250, 20);
            GUI.Box(progressRect, "");
            GUI.Box(new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height), "", "button");
            
            if (GUILayout.Button("Stop Processing"))
            {
                StopBatchProcessing();
            }
        }
        else
        {
            if (GUILayout.Button("Start Batch Processing"))
            {
                StartBatchProcessing();
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Validates if the required components and settings are properly configured
    /// </summary>
    /// <returns>True if everything is configured correctly, false otherwise</returns>
    public bool ValidateConfiguration()
    {
        if (acoTrainer == null)
        {
            LogError("ACOTrainer component is missing!");
            return false;
        }
        
        if (string.IsNullOrEmpty(inputDirectory))
        {
            LogError("Input directory is not set!");
            return false;
        }
        
        string directoryPath = useAbsolutePath ? inputDirectory : Path.Combine(Application.streamingAssetsPath, inputDirectory);
        if (!Directory.Exists(directoryPath))
        {
            LogError($"Input directory does not exist: {directoryPath}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets detailed statistics about the current batch processing session
    /// </summary>
    /// <returns>Statistics string</returns>
    public string GetBatchStatistics()
    {
        if (totalFiles == 0)
            return "No batch processing session active";
            
        float progressPercent = GetProgress() * 100f;
        string currentFile = string.IsNullOrEmpty(currentProcessingFile) ? "None" : Path.GetFileNameWithoutExtension(currentProcessingFile);
        
        return $"Processing Statistics:\n" +
               $"Total Files: {totalFiles}\n" +
               $"Processed: {processedCount}\n" +
               $"Progress: {progressPercent:F1}%\n" +
               $"Current File: {currentFile}\n" +
               $"Status: {GetCurrentStatus()}";
    }
}
