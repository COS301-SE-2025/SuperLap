using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrackMaster : MonoBehaviour
{
    [Header("Track Master Settings")]
    [SerializeField] private int meshResolution = 1000;
    [SerializeField] private int splitCount = 50;
    [SerializeField] private float splitMeshScale = 25f;
    [SerializeField] private Material splitMaterial;


    [Header("Checkpoint Settings")]
    [SerializeField] private GameObject checkpointPrefab;
    [SerializeField] private Material[] checkpointMaterials = new Material[3];

    [Header("Raceline Visualization")]
    [SerializeField] private bool showRaceline = true;
    [SerializeField] private Color racelineColor = Color.red;
    [SerializeField] private float racelineWidth = 0.5f;
    [SerializeField] private float racelineHeightOffset = 0.1f;

    [Header("Agent Spawning")]
    [SerializeField] private GameObject motorcycleAgentPrefab;
    [SerializeField] private float agentSpawnHeight = 1.0f;
    [SerializeField] private int startingPositionIndex = 0; // Index along raceline for starting position
    [SerializeField] private bool spawnAtRandomPosition = false;

    [Header("Player Mode")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject screen;


    public static TrackMaster instance;
    private static List<Vector2> currentRaceline;
    private static GameObject spawnedAgent;
    private static LineRenderer racelineRenderer;

    void Start()
    {
        instance = this;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P) && spawnedAgent != null)
        {
            // switch to player mode
            if (mainCamera != null)
            {
                mainCamera.gameObject.SetActive(false);
                screen.SetActive(false);
                Debug.Log("Switched to Player Mode with camera: " + mainCamera.name);
            }
            else
            {
                Debug.LogWarning("Main camera not assigned in TrackMaster!");
            }
        }
    }

    public static void LoadTrack(TrackImageProcessor.ProcessingResults results)
    {
        // Store raceline for agent spawning
        currentRaceline = results.raceline;

        (List<Vector2> innerPoints, List<Vector2> outerPoints) = Processor3D.GetNewBoundaries(results, instance.meshResolution);

        Mesh mesh = Processor3D.GenerateOutputMesh(innerPoints, outerPoints);
        MeshFilter meshFilter = instance.gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = instance.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material.color = Color.white;
        instance.GetComponent<MeshFilter>().mesh = mesh;

        // Add mesh collider to the track
        MeshCollider meshCollider = instance.gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        CreateCheckpoints(results.raceline);
        CreateRacelineVisualization(results.raceline);

        // Spawn the motorcycle agent on the raceline
        //SpawnMotorcycleAgent();

        AssetDatabase.CreateAsset(mesh, "Assets/GeneratedMeshes/TrackMesh.asset");
        // AssetDatabase.SaveAssets();
    }

    private static void CreateCheckpoints(List<Vector2> raceline)
    {
        if (instance.checkpointPrefab == null)
        {
            Debug.LogWarning("Checkpoint prefab not assigned in TrackMaster!");
            return;
        }

        // Create checkpoints at split points
        int checkpointId = 0;
        for (int i = 0; i < raceline.Count; i += instance.meshResolution / instance.splitCount)
        {
            Vector2 point = raceline[i];
            GameObject checkpointObj = Instantiate(instance.checkpointPrefab);
            checkpointObj.transform.position = new Vector3(point.x, instance.agentSpawnHeight, point.y);
            checkpointObj.transform.SetParent(instance.transform);
            checkpointObj.name = $"Checkpoint_{checkpointId}";

            // Configure the checkpoint component
            Checkpoint checkpoint = checkpointObj.GetComponent<Checkpoint>();
            if (checkpoint != null)
            {
                checkpoint.SetCheckpointId(checkpointId);
            }
            else
            {
                // Add checkpoint component if it doesn't exist
                checkpoint = checkpointObj.AddComponent<Checkpoint>();
                checkpoint.SetCheckpointId(checkpointId);
            }

            checkpointId++;
        }

        // Configure the checkpoint manager materials
        CheckpointManager manager = FindAnyObjectByType<CheckpointManager>();
        if (manager != null && instance.checkpointMaterials != null && instance.checkpointMaterials.Length > 0)
        {
            manager.SetCheckpointMaterials(instance.checkpointMaterials);
        }
    }

    private static void CreateRacelineVisualization(List<Vector2> raceline)
    {
        if (!instance.showRaceline || raceline == null || raceline.Count == 0)
        {
            if (racelineRenderer != null)
            {
                racelineRenderer.enabled = false;
            }
            return;
        }

        // Create or get the LineRenderer component
        if (racelineRenderer == null)
        {
            GameObject racelineObject = new GameObject("Raceline");
            racelineObject.transform.SetParent(instance.transform);
            racelineRenderer = racelineObject.AddComponent<LineRenderer>();
        }

        // Configure the LineRenderer
        SetupRacelineRenderer();

        // Convert 2D raceline points to 3D positions with height offset
        Vector3[] racelinePoints = new Vector3[raceline.Count + 1]; // +1 to close the loop
        
        for (int i = 0; i < raceline.Count; i++)
        {
            Vector2 point = raceline[i];
            racelinePoints[i] = new Vector3(point.x, instance.racelineHeightOffset, point.y);
        }
        
        // Close the loop by connecting back to the first point
        racelinePoints[raceline.Count] = racelinePoints[0];

        // Apply the points to the LineRenderer
        racelineRenderer.positionCount = racelinePoints.Length;
        racelineRenderer.SetPositions(racelinePoints);
        racelineRenderer.enabled = true;

        Debug.Log($"Created raceline visualization with {raceline.Count} points");
    }

    private static void SetupRacelineRenderer()
    {
        if (racelineRenderer == null) return;

        // Create a material for the line if it doesn't exist
        Material racelineMaterial = new Material(Shader.Find("Sprites/Default"));
        racelineMaterial.color = instance.racelineColor;

        // Configure LineRenderer properties
        racelineRenderer.material = racelineMaterial;
        racelineRenderer.material.color = instance.racelineColor;
        racelineRenderer.startWidth = instance.racelineWidth;
        racelineRenderer.endWidth = instance.racelineWidth;
        racelineRenderer.useWorldSpace = true;
        racelineRenderer.loop = false; // We manually close the loop
        racelineRenderer.sortingOrder = 1; // Render on top of track
    }

    private static void SpawnMotorcycleAgent()
    {
        if (instance.motorcycleAgentPrefab == null)
        {
            Debug.LogWarning("MotorcycleAgent prefab not assigned in TrackMaster!");
            return;
        }

        if (currentRaceline == null || currentRaceline.Count == 0)
        {
            Debug.LogWarning("No raceline available for agent spawning!");
            return;
        }

        // Destroy existing agent if present
        if (spawnedAgent != null)
        {
            DestroyImmediate(spawnedAgent);
        }

        // Determine starting position
        Vector3 startPosition = GetStartingPosition();
        Vector3 startDirection = GetStartingDirection();

        // Spawn the agent
        spawnedAgent = Instantiate(instance.motorcycleAgentPrefab, startPosition, Quaternion.LookRotation(startDirection));
        spawnedAgent.name = "SpawnedMotorcycleAgent";
        spawnedAgent.transform.SetParent(instance.transform);

        // Configure the agent's starting position if it has a MotorcycleAgent component
        MotorcycleAgent agentComponent = spawnedAgent.GetComponent<MotorcycleAgent>();
        if (agentComponent != null)
        {
            // Set the starting position for ML-Agents episode resets
            var startingPositionField = typeof(MotorcycleAgent).GetField("startingPosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var startingRotationField = typeof(MotorcycleAgent).GetField("startingRotation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (startingPositionField != null)
                startingPositionField.SetValue(agentComponent, startPosition);
            if (startingRotationField != null)
                startingRotationField.SetValue(agentComponent, startDirection);
        }

        Debug.Log($"Spawned MotorcycleAgent at position: {startPosition} with direction: {startDirection}");
    }

    private static Vector3 GetStartingPosition()
    {
        int positionIndex;

        if (instance.spawnAtRandomPosition)
        {
            positionIndex = Random.Range(0, currentRaceline.Count);
        }
        else
        {
            positionIndex = Mathf.Clamp(instance.startingPositionIndex, 0, currentRaceline.Count - 1);
        }

        Vector2 racelinePoint = currentRaceline[positionIndex];
        return new Vector3(racelinePoint.x, instance.agentSpawnHeight, racelinePoint.y);
    }

    private static Vector3 GetStartingDirection()
    {
        if (currentRaceline.Count < 2)
        {
            return Vector3.forward;
        }

        // Get the starting index directly from our configuration
        int startIndex;
        if (instance.spawnAtRandomPosition)
        {
            startIndex = Random.Range(0, currentRaceline.Count);
        }
        else
        {
            startIndex = Mathf.Clamp(instance.startingPositionIndex, 0, currentRaceline.Count - 1);
        }

        // Look ahead several points for smoother direction calculation
        int lookAheadDistance = Mathf.Max(1, currentRaceline.Count / 20); // ~5% of track length
        int endIndex = (startIndex + lookAheadDistance) % currentRaceline.Count;

        // Get two points for direction calculation
        Vector2 point1 = currentRaceline[startIndex];
        Vector2 point2 = currentRaceline[endIndex];

        // Calculate direction vector
        Vector2 direction2D = point2 - point1;
        
        // Handle case where points are too close (circular track wrap-around)
        if (direction2D.magnitude < 0.1f)
        {
            endIndex = (startIndex + 1) % currentRaceline.Count;
            point2 = currentRaceline[endIndex];
            direction2D = point2 - point1;
        }

        // Convert to 3D and normalize
        Vector3 direction3D = new Vector3(direction2D.x, 0, direction2D.y).normalized;
        
        return direction3D != Vector3.zero ? direction3D : Vector3.forward;
    }

    /// <summary>
    /// Public method to manually spawn or respawn the motorcycle agent
    /// </summary>
    public static void RespawnAgent()
    {
        if (currentRaceline != null && currentRaceline.Count > 0)
        {
            SpawnMotorcycleAgent();
        }
        else
        {
            Debug.LogWarning("Cannot respawn agent: No track loaded!");
        }
    }

    /// <summary>
    /// Public method to set a new starting position index
    /// </summary>
    /// <param name="newIndex">Index along the raceline for the new starting position</param>
    public static void SetStartingPosition(int newIndex)
    {
        if (instance != null)
        {
            instance.startingPositionIndex = Mathf.Clamp(newIndex, 0, 
                currentRaceline != null ? currentRaceline.Count - 1 : 0);
        }
    }

    /// <summary>
    /// Public method to get the currently spawned agent
    /// </summary>
    /// <returns>The spawned MotorcycleAgent GameObject, or null if none exists</returns>
    public static GameObject GetSpawnedAgent()
    {
        return spawnedAgent;
    }

    /// <summary>
    /// Public method to get the current optimal raceline
    /// </summary>
    /// <returns>The current raceline as a list of Vector2 points, or null if no track is loaded</returns>
    public static List<Vector2> GetCurrentRaceline()
    {
        return currentRaceline;
    }
}
