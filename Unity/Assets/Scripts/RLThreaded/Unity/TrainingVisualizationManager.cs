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
/// Statistics about visualization system performance
/// </summary>
public struct VisualizationStats
{
    public int activeVisualizations;
    public int pooledObjects;
    public int totalCreated;
}