using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages real-time visualization of training agents in Unity
/// Handles agent representation, trails, and performance indicators
/// </summary>
public class TrainingVisualizationManager
{
    private readonly GameObject agentPrefab;
    private readonly Transform parentContainer;
    private readonly int maxVisualizedAgents;
    
    // Agent visualization objects
    private Dictionary<int, AgentVisualizationObject> activeVisualizations;
    private Queue<AgentVisualizationObject> inactivePool;
    
    // Visualization settings
    private bool showTrails = false;
    private bool showPerformanceIndicators = true;
    
    public TrainingVisualizationManager(GameObject agentPrefab, Transform parentContainer, int maxAgents = 50)
    {
        this.agentPrefab = agentPrefab;
        this.parentContainer = parentContainer;
        this.maxVisualizedAgents = maxAgents;
        
        this.activeVisualizations = new Dictionary<int, AgentVisualizationObject>();
        this.inactivePool = new Queue<AgentVisualizationObject>();
        
        InitializePool();
    }
    
    /// <summary>
    /// Update agent visualizations with current training data
    /// </summary>
    public void UpdateAgentVisualizations(List<AgentVisualizationData> agentData)
    {
        // Limit the number of visualized agents for performance
        var visualizedData = agentData.Take(maxVisualizedAgents).ToList();
        
        // Update existing visualizations
        var currentAgentIds = new HashSet<int>();
        
        foreach (var data in visualizedData)
        {
            currentAgentIds.Add(data.agentId);
            
            if (activeVisualizations.TryGetValue(data.agentId, out AgentVisualizationObject visualization))
            {
                // Update existing visualization
                visualization.UpdateData(data);
            }
            else
            {
                // Create new visualization
                CreateVisualization(data);
            }
        }
        
        // Remove visualizations for agents that are no longer active
        var agentsToRemove = activeVisualizations.Keys.Except(currentAgentIds).ToList();
        foreach (var agentId in agentsToRemove)
        {
            RemoveVisualization(agentId);
        }
    }
    
    /// <summary>
    /// Set whether to show agent trails
    /// </summary>
    public void SetShowTrails(bool show)
    {
        showTrails = show;
        
        foreach (var visualization in activeVisualizations.Values)
        {
            visualization.SetTrailEnabled(show);
        }
    }
    
    /// <summary>
    /// Set whether to show performance indicators
    /// </summary>
    public void SetShowPerformanceIndicators(bool show)
    {
        showPerformanceIndicators = show;
        
        foreach (var visualization in activeVisualizations.Values)
        {
            visualization.SetPerformanceIndicatorsEnabled(show);
        }
    }
    
    /// <summary>
    /// Clear all visualizations
    /// </summary>
    public void ClearAll()
    {
        var agentIds = activeVisualizations.Keys.ToList();
        foreach (var agentId in agentIds)
        {
            RemoveVisualization(agentId);
        }
    }
    
    /// <summary>
    /// Get statistics about current visualizations
    /// </summary>
    public VisualizationStats GetStats()
    {
        return new VisualizationStats
        {
            activeVisualizations = activeVisualizations.Count,
            pooledObjects = inactivePool.Count,
            totalCreated = activeVisualizations.Count + inactivePool.Count
        };
    }
    
    #region Private Methods
    
    private void InitializePool()
    {
        // Pre-create some visualization objects for performance
        int initialPoolSize = Mathf.Min(maxVisualizedAgents / 2, 25);
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            var obj = CreateVisualizationObject();
            obj.gameObject.SetActive(false);
            inactivePool.Enqueue(obj);
        }
    }
    
    private void CreateVisualization(AgentVisualizationData data)
    {
        AgentVisualizationObject visualization;
        
        // Try to get from pool first
        if (inactivePool.Count > 0)
        {
            visualization = inactivePool.Dequeue();
            visualization.gameObject.SetActive(true);
        }
        else
        {
            visualization = CreateVisualizationObject();
        }
        
        // Initialize and add to active list
        visualization.Initialize(data);
        visualization.SetTrailEnabled(showTrails);
        visualization.SetPerformanceIndicatorsEnabled(showPerformanceIndicators);
        
        activeVisualizations[data.agentId] = visualization;
    }
    
    private void RemoveVisualization(int agentId)
    {
        if (activeVisualizations.TryGetValue(agentId, out AgentVisualizationObject visualization))
        {
            activeVisualizations.Remove(agentId);
            
            // Return to pool
            visualization.gameObject.SetActive(false);
            visualization.Reset();
            inactivePool.Enqueue(visualization);
        }
    }
    
    private AgentVisualizationObject CreateVisualizationObject()
    {
        GameObject obj = Object.Instantiate(agentPrefab, parentContainer);
        
        // Add or get the visualization component
        var visualizationComponent = obj.GetComponent<AgentVisualizationObject>();
        if (visualizationComponent == null)
        {
            visualizationComponent = obj.AddComponent<AgentVisualizationObject>();
        }
        
        return visualizationComponent;
    }
    
    #endregion
}

/// <summary>
/// Individual agent visualization object
/// Handles the visual representation of a single training agent
/// </summary>
public class AgentVisualizationObject : MonoBehaviour
{
    [Header("Visualization Components")]
    [SerializeField] private Transform agentModel;
    [SerializeField] private LineRenderer trailRenderer;
    [SerializeField] private Canvas performanceCanvas;
    [SerializeField] private TMPro.TextMeshProUGUI speedText;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;
    [SerializeField] private Renderer agentRenderer;
    
    [Header("Trail Settings")]
    [SerializeField] private int maxTrailPoints = 100;
    [SerializeField] private float trailUpdateDistance = 0.5f;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color offTrackColor = Color.red;
    [SerializeField] private Color completedColor = Color.blue;
    
    // State tracking
    private AgentVisualizationData currentData;
    private List<Vector3> trailPoints;
    private Vector3 lastTrailPosition;
    private bool isInitialized = false;
    
    void Awake()
    {
        // Initialize components if not assigned
        if (agentModel == null)
            agentModel = transform;
        
        if (trailRenderer == null)
            trailRenderer = GetComponent<LineRenderer>();
        
        if (agentRenderer == null)
            agentRenderer = GetComponent<Renderer>();
        
        // Initialize trail
        trailPoints = new List<Vector3>();
        
        // Setup trail renderer
        SetupTrailRenderer();
        
        // Setup performance canvas
        SetupPerformanceUI();
    }
    
    /// <summary>
    /// Initialize with agent data
    /// </summary>
    public void Initialize(AgentVisualizationData data)
    {
        currentData = data;
        isInitialized = true;
        
        // Set initial position
        Vector3 worldPosition = new Vector3(data.position.x, 0, data.position.y);
        transform.position = worldPosition;
        
        // Set initial rotation
        Vector3 worldDirection = new Vector3(data.direction.x, 0, data.direction.y);
        if (worldDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(worldDirection);
        }
        
        // Initialize trail
        ResetTrail();
        
        // Update visual state
        UpdateVisualState();
    }
    
    /// <summary>
    /// Update with new agent data
    /// </summary>
    public void UpdateData(AgentVisualizationData data)
    {
        if (!isInitialized)
        {
            Initialize(data);
            return;
        }
        
        currentData = data;
        
        // Update position and rotation
        Vector3 worldPosition = new Vector3(data.position.x, 0, data.position.y);
        Vector3 worldDirection = new Vector3(data.direction.x, 0, data.direction.y);
        
        transform.position = worldPosition;
        
        if (worldDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(worldDirection);
        }
        
        // Update trail
        UpdateTrail(worldPosition);
        
        // Update visual state
        UpdateVisualState();
        
        // Update performance UI
        UpdatePerformanceUI();
    }
    
    /// <summary>
    /// Enable or disable trail rendering
    /// </summary>
    public void SetTrailEnabled(bool enabled)
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = enabled;
        }
    }
    
    /// <summary>
    /// Enable or disable performance indicators
    /// </summary>
    public void SetPerformanceIndicatorsEnabled(bool enabled)
    {
        if (performanceCanvas != null)
        {
            performanceCanvas.gameObject.SetActive(enabled);
        }
    }
    
    /// <summary>
    /// Reset the visualization object
    /// </summary>
    public void Reset()
    {
        isInitialized = false;
        ResetTrail();
        
        if (performanceCanvas != null)
        {
            performanceCanvas.gameObject.SetActive(false);
        }
    }
    
    #region Private Methods
    
    private void SetupTrailRenderer()
    {
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.material.color = normalColor;
        trailRenderer.startWidth = 0.1f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.useWorldSpace = true;
        trailRenderer.positionCount = 0;
    }
    
    private void SetupPerformanceUI()
    {
        if (performanceCanvas == null)
        {
            // Create performance UI if it doesn't exist
            GameObject canvasObj = new GameObject("PerformanceUI");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.up * 2f;
            
            performanceCanvas = canvasObj.AddComponent<Canvas>();
            performanceCanvas.renderMode = RenderMode.WorldSpace;
            performanceCanvas.worldCamera = Camera.main;
            
            var canvasScaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.scaleFactor = 0.01f;
            
            // Create speed text
            CreatePerformanceText();
        }
    }
    
    private void CreatePerformanceText()
    {
        if (speedText == null)
        {
            GameObject speedObj = new GameObject("SpeedText");
            speedObj.transform.SetParent(performanceCanvas.transform);
            speedObj.transform.localPosition = Vector3.zero;
            speedObj.transform.localScale = Vector3.one;
            
            speedText = speedObj.AddComponent<TMPro.TextMeshProUGUI>();
            speedText.text = "0 km/h";
            speedText.fontSize = 24;
            speedText.alignment = TMPro.TextAlignmentOptions.Center;
            speedText.color = Color.white;
        }
        
        if (statusText == null)
        {
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(performanceCanvas.transform);
            statusObj.transform.localPosition = Vector3.down * 30;
            statusObj.transform.localScale = Vector3.one;
            
            statusText = statusObj.AddComponent<TMPro.TextMeshProUGUI>();
            statusText.text = "Active";
            statusText.fontSize = 18;
            statusText.alignment = TMPro.TextAlignmentOptions.Center;
            statusText.color = Color.white;
        }
    }
    
    private void UpdateVisualState()
    {
        if (agentRenderer == null)
            return;
        
        // Set color based on agent state
        Color targetColor = normalColor;
        
        if (!currentData.isActive)
        {
            targetColor = Color.gray;
        }
        else if (currentData.isOffTrack)
        {
            targetColor = offTrackColor;
        }
        else if (currentData.completionPercentage > 0.9f)
        {
            targetColor = completedColor;
        }
        
        agentRenderer.material.color = targetColor;
        
        // Update trail color
        if (trailRenderer != null)
        {
            trailRenderer.material.color = targetColor;
        }
    }
    
    private void UpdateTrail(Vector3 position)
    {
        if (trailRenderer == null)
            return;
        
        // Add point if moved far enough
        if (Vector3.Distance(position, lastTrailPosition) > trailUpdateDistance)
        {
            trailPoints.Add(position);
            lastTrailPosition = position;
            
            // Limit trail length
            if (trailPoints.Count > maxTrailPoints)
            {
                trailPoints.RemoveAt(0);
            }
            
            // Update renderer
            trailRenderer.positionCount = trailPoints.Count;
            trailRenderer.SetPositions(trailPoints.ToArray());
        }
    }
    
    private void ResetTrail()
    {
        trailPoints.Clear();
        lastTrailPosition = transform.position;
        
        if (trailRenderer != null)
        {
            trailRenderer.positionCount = 0;
        }
    }
    
    private void UpdatePerformanceUI()
    {
        if (speedText != null)
        {
            float speedKmh = currentData.speed * 3.6f;
            speedText.text = $"{speedKmh:F1} km/h";
        }
        
        if (statusText != null)
        {
            string status = currentData.isActive ? "Active" : "Inactive";
            if (currentData.isOffTrack)
                status = "Off Track";
            
            statusText.text = status;
        }
        
        // Make UI face camera
        if (performanceCanvas != null && Camera.main != null)
        {
            performanceCanvas.transform.LookAt(Camera.main.transform);
            performanceCanvas.transform.Rotate(0, 180, 0);
        }
    }
    
    #endregion
}

/// <summary>
/// Statistics about visualization system performance
/// </summary>
public struct VisualizationStats
{
    public int activeVisualizations;
    public int pooledObjects;
    public int totalCreated;
}