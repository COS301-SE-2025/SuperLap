using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrackMaster : MonoBehaviour
{
    [Header("Track Master Settings")]
    [SerializeField] private int meshResolution = 1000;
    [SerializeField] private int splitCount = 50;
    [SerializeField] private float splitMeshScale = 25f;

    [Header("Agent Spawning")]
    [SerializeField] private GameObject motorcycleAgentPrefab;
    [SerializeField] private float agentSpawnHeight = 1.0f;
    [SerializeField] private int startingPositionIndex = 0; // Index along raceline for starting position
    [SerializeField] private bool spawnAtRandomPosition = false;

    public static TrackMaster instance;
    private static List<Vector2> currentRaceline;
    private static GameObject spawnedAgent;

    void Start()
    {
        instance = this;
    }

    void Update()
    {

    }

    public static void LoadTrack(TrackImageProcessor.ProcessingResults results)
    {
        // Store raceline for agent spawning
        currentRaceline = results.raceline;

        Mesh mesh = Processor3D.GenerateOutputMesh(results, instance.meshResolution);
        MeshFilter meshFilter = instance.gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = instance.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material.color = Color.white;
        instance.GetComponent<MeshFilter>().mesh = mesh;

        // Add mesh collider to the track
        MeshCollider meshCollider = instance.gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        CreateSplits(results.raceline);

        // Spawn the motorcycle agent on the raceline
        SpawnMotorcycleAgent();

        AssetDatabase.CreateAsset(mesh, "Assets/GeneratedMeshes/TrackMesh.asset");
        // AssetDatabase.SaveAssets();
    }

    private static void CreateSplits(List<Vector2> raceline)
    {
        // Create red spheres at split points
        for (int i = 0; i < raceline.Count; i += instance.meshResolution / instance.splitCount)
        {
            Vector2 point = raceline[i];
            GameObject splitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            splitPoint.transform.position = new Vector3(point.x, 0, point.y);
            splitPoint.transform.localScale = Vector3.one * instance.splitMeshScale; // Adjust size as needed
            splitPoint.GetComponent<Renderer>().material.color = Color.red;
            splitPoint.GetComponent<Collider>().isTrigger = true; // Make collider a trigger
            splitPoint.transform.SetParent(instance.transform); // Set parent to keep hierarchy clean
            splitPoint.name = $"SplitPoint_{i}";
        }
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
        Vector3 startDirection = GetStartingDirection(startPosition);

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

    private static Vector3 GetStartingDirection(Vector3 currentPosition)
    {
        if (currentRaceline.Count < 2)
        {
            return Vector3.forward; // Default direction if not enough points
        }

        // Find the closest raceline point to our current position
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < currentRaceline.Count; i++)
        {
            Vector2 point = currentRaceline[i];
            Vector3 worldPoint = new Vector3(point.x, instance.agentSpawnHeight, point.y);
            float distance = Vector3.Distance(currentPosition, worldPoint);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        // Get direction to next point on raceline
        int nextIndex = (closestIndex + 1) % currentRaceline.Count;
        Vector2 currentPoint = currentRaceline[closestIndex];
        Vector2 nextPoint = currentRaceline[nextIndex];

        Vector3 direction = new Vector3(nextPoint.x - currentPoint.x, 0, nextPoint.y - currentPoint.y).normalized;
        return direction != Vector3.zero ? direction : Vector3.forward;
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
}
