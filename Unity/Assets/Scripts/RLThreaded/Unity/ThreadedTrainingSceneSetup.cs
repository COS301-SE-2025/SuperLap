using UnityEngine;

/// <summary>
/// Scene setup helper for the multithreaded training system
/// Run this in Unity Editor to automatically set up the required GameObjects
/// </summary>
public class ThreadedTrainingSceneSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private Canvas uiCanvas;
    
    [ContextMenu("Setup Threaded Training Scene")]
    public void SetupScene()
    {
        // 1. Create TrainingEngine GameObject
        CreateTrainingEngine();
        
        // 2. Create Agent Container
        CreateAgentContainer();
        
        // 3. Create UI System
        CreateUISystem();
        
        Debug.Log("Threaded training scene setup complete!");
    }
    
    private void CreateTrainingEngine()
    {
        GameObject trainingEngineGO = new GameObject("ThreadedTrainingEngine");
        var engineInterface = trainingEngineGO.AddComponent<TrainingEngineInterface>();
        
        // Configure with default settings
        var agentContainer = GameObject.Find("AgentContainer");
        if (agentContainer == null)
        {
            agentContainer = new GameObject("AgentContainer");
        }
        
        // Set references via reflection or public fields
        Debug.Log("Created TrainingEngine GameObject - configure the agent prefab reference in inspector");
    }
    
    private void CreateAgentContainer()
    {
        if (GameObject.Find("AgentContainer") == null)
        {
            GameObject container = new GameObject("AgentContainer");
            container.transform.position = Vector3.zero;
            Debug.Log("Created AgentContainer GameObject");
        }
    }
    
    private void CreateUISystem()
    {
        GameObject uiGO = new GameObject("ThreadedTrainingUI");
        var uiController = uiGO.AddComponent<ThreadedTrainingUIController>();
        
        Debug.Log("Created Training UI GameObject - you'll need to create the UI elements manually or via prefab");
    }
}