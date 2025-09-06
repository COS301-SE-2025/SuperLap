using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ACOTrackMaster : MonoBehaviour
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


    public static ACOTrackMaster instance;
    private static List<System.Numerics.Vector2> currentRaceline;
    private static PolygonTrack track;

    private static GameObject spawnedAgent;
    private static LineRenderer racelineRenderer;

    // Event to notify when track is loaded
    public static System.Action OnTrackLoaded;

    void Start()
    {
        instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && spawnedAgent != null)
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
        List<System.Numerics.Vector2> vector2s = new List<System.Numerics.Vector2>();
        foreach (var vec in results.raceline)
        {
            vector2s.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        currentRaceline = vector2s;

        // Create PolygonTrack for boundary checks
        // NOTE: Fixed boundary swap - results.innerBoundary is actually the outer boundary
        List<System.Numerics.Vector2> inner = new List<System.Numerics.Vector2>();
        List<System.Numerics.Vector2> outer = new List<System.Numerics.Vector2>();
        foreach (var vec in results.outerBoundary) // This is actually the inner boundary
        {
            inner.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        foreach (var vec in results.innerBoundary) // This is actually the outer boundary
        {
            outer.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        track = new PolygonTrack(outer.ToArray(), inner.ToArray());

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

        // CreateCheckpoints(results);
        CreateRacelineVisualization(results.raceline);

        // Initialize the RacelineAnalyzer with the raceline data for optimized queries
        RacelineAnalyzer.Initialize(results.raceline);

        // Spawn the motorcycle agent on the raceline
        //SpawnMotorcycleAgent();

        // AssetDatabase.CreateAsset(mesh, "Assets/GeneratedMeshes/TrackMesh.asset");
        // AssetDatabase.SaveAssets();

        // Notify that track has been loaded
        OnTrackLoaded?.Invoke();
        Debug.Log("Track loaded and processed - notifying listeners");
    }

    public static Vector3 GetTrainingSpawnPosition(int agentIndex, List<System.Numerics.Vector2> raceline)
    {
        if (raceline == null || raceline.Count == 0)
        {
            return Vector3.zero;
        }

        // Spread agents around the track
        float spacing = (float)raceline.Count / 4f; // 4 environments max
        int positionIndex = Mathf.RoundToInt(agentIndex * spacing) % raceline.Count;

        System.Numerics.Vector2 racelinePoint = raceline[positionIndex];
        Vector3 basePosition = new Vector3(racelinePoint.X, instance.agentSpawnHeight, racelinePoint.Y);

        // Add lateral offset to prevent collisions
        Vector3 trackDirection = GetTrainingSpawnDirection(agentIndex, raceline);
        Vector3 lateralOffset = Vector3.Cross(trackDirection, Vector3.up) * (agentIndex - 1.5f) * 3f;

        return basePosition + lateralOffset;
    }

    public static Vector3 GetTrainingSpawnDirection(int agentIndex, List<System.Numerics.Vector2> raceline)
    {
        if (raceline == null || raceline.Count == 0)
        {
            return Vector3.forward;
        }

        float spacing = (float)raceline.Count / 4f;
        int positionIndex = Mathf.RoundToInt(agentIndex * spacing) % raceline.Count;

        // Look ahead for direction
        int lookAheadDistance = Mathf.Max(1, raceline.Count / 20);
        int endIndex = (positionIndex + lookAheadDistance) % raceline.Count;

        System.Numerics.Vector2 point1 = raceline[positionIndex];
        System.Numerics.Vector2 point2 = raceline[endIndex];
        System.Numerics.Vector2 direction2D = point2 - point1;

        if (direction2D.Length() < 0.1f)
        {
            endIndex = (positionIndex + 1) % raceline.Count;
            point2 = raceline[endIndex];
            direction2D = point2 - point1;
        }

        Vector3 direction3D = new Vector3(direction2D.X, 0, direction2D.Y).normalized;
        return direction3D != Vector3.zero ? direction3D : Vector3.forward;
    }

    // private static void CreateCheckpoints(TrackImageProcessor.ProcessingResults racelineResult)
    // {
    //     if (instance.checkpointPrefab == null)
    //     {
    //         Debug.LogWarning("Checkpoint prefab not assigned in TrackMaster!");
    //         return;
    //     }

    //     List<Vector2> raceline = racelineResult.raceline;

    //     // Create checkpoints at split points
    //     int checkpointId = 0;
    //     for (int i = 0; i < raceline.Count; i += instance.meshResolution / instance.splitCount)
    //     {
    //         Vector2 point = raceline[i];
    //         GameObject checkpointObj = Instantiate(instance.checkpointPrefab);
    //         checkpointObj.transform.position = new Vector3(point.x, instance.agentSpawnHeight, point.y);
    //         checkpointObj.transform.SetParent(instance.transform);
    //         checkpointObj.name = $"Checkpoint_{checkpointId}";

    //         // Configure the checkpoint component
    //         Checkpoint checkpoint = checkpointObj.GetComponent<Checkpoint>();
    //         if (checkpoint != null)
    //         {
    //             checkpoint.SetCheckpointId(checkpointId);
    //         }
    //         else
    //         {
    //             // Add checkpoint component if it doesn't exist
    //             checkpoint = checkpointObj.AddComponent<Checkpoint>();
    //             checkpoint.SetCheckpointId(checkpointId);
    //         }

    //         checkpointId++;
    //     }

    //     // Configure the checkpoint manager materials
    //     CheckpointManager manager = FindAnyObjectByType<CheckpointManager>();
    //     if (manager != null && instance.checkpointMaterials != null && instance.checkpointMaterials.Length > 0)
    //     {
    //         manager.SetCheckpointMaterials(instance.checkpointMaterials);
    //     }
    // }

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

        System.Numerics.Vector2 racelinePoint = currentRaceline[positionIndex];
        return new Vector3(racelinePoint.X, instance.agentSpawnHeight, racelinePoint.Y);
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
        System.Numerics.Vector2 point1 = currentRaceline[startIndex];
        System.Numerics.Vector2 point2 = currentRaceline[endIndex];

        // Calculate direction vector
        System.Numerics.Vector2 direction2D = point2 - point1;

        // Handle case where points are too close (circular track wrap-around)
        if (direction2D.Length() < 0.1f)
        {
            endIndex = (startIndex + 1) % currentRaceline.Count;
            point2 = currentRaceline[endIndex];
            direction2D = point2 - point1;
        }

        // Convert to 3D and normalize
        Vector3 direction3D = new Vector3(direction2D.X, 0, direction2D.Y).normalized;

        return direction3D != Vector3.zero ? direction3D : Vector3.forward;
    }

    public static List<System.Numerics.Vector2> GetCurrentRaceline()
    {
        return currentRaceline;
    }

    public static PolygonTrack GetPolygonTrack()
    {
        return track;
    }
    
}
