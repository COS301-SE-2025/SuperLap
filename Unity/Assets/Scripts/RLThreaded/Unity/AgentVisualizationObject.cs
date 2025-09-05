using UnityEngine;
using System.Collections.Generic;

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