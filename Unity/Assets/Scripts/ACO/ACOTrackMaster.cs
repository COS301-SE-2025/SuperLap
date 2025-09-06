using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class ACOTrackMaster : MonoBehaviour
{
    [Header("Track Master Settings")]
    [SerializeField] private int meshResolution = 1000;
    [SerializeField] private int splitCount = 50;
    [SerializeField] private float splitMeshScale = 25f;
    [SerializeField] private Material splitMaterial;

    [Header("Raceline Visualization")]
    [SerializeField] private bool showRaceline = true;
    [SerializeField] private Color racelineColor = Color.red;
    [SerializeField] private float racelineWidth = 0.5f;
    [SerializeField] private float racelineHeightOffset = 0.1f;

    [Header("Agent Spawning")]
    [SerializeField] private float agentSpawnHeight = 1.0f;

    [Header("Player Mode")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject screen;

    [Header("Track Loading")]
    [SerializeField] private bool loadTestTrack = false;
    [SerializeField] private string trackDataFileName = "saved_track_data.txt";


    public static ACOTrackMaster instance;
    private static List<System.Numerics.Vector2> currentRaceline;
    private static PolygonTrack track;

    private static GameObject spawnedAgent;
    private static LineRenderer racelineRenderer;
    private static Dictionary<int, LineRenderer> agentTrajectoryRenderers = new Dictionary<int, LineRenderer>();
    private static List<ACOAgent> trackedAgents = new List<ACOAgent>();

    // Event to notify when track is loaded
    public static System.Action OnTrackLoaded;

    void Start()
    {
        instance = this;
        
        // Check if we should load a test track from saved data
        if (loadTestTrack)
        {
            var savedTrackData = LoadTrackDataFromFile();
            if (savedTrackData != null)
            {
                Debug.Log("Loading track from saved data...");
                LoadTrack(savedTrackData);
            }
            else
            {
                Debug.LogWarning("loadTestTrack is enabled but no saved track data found or failed to load.");
            }
        }
    }

    void Update()
    {
        // Update agent trajectories in real-time       
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
        List < System.Numerics.Vector2 > vector2s = new List<System.Numerics.Vector2>();
        foreach (var vec in results.raceline)
        {
            vector2s.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        currentRaceline = vector2s;

        Debug.Log($"Loaded track with {results.raceline.Count} raceline points, " +
                  $"{results.innerBoundary.Count} outer boundary points, " +
                  $"{results.outerBoundary.Count} inner boundary points.");
        (List<Vector2> innerPoints, List<Vector2> outerPoints) = Processor3D.GetNewBoundaries(results, instance.meshResolution);
        Debug.Log($"Resampled boundaries to {innerPoints.Count} inner points and {outerPoints.Count} outer points.");


        // Create PolygonTrack for boundary checks
        // NOTE: Fixed boundary swap - results.innerBoundary is actually the outer boundary
        List<System.Numerics.Vector2> inner = new List<System.Numerics.Vector2>();
        List<System.Numerics.Vector2> outer = new List<System.Numerics.Vector2>();
        foreach (var vec in outerPoints) // This is actually the inner boundary
        {
            inner.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        foreach (var vec in innerPoints) // This is actually the outer boundary
        {
            outer.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        
        track = new PolygonTrack(outer.ToArray(), inner.ToArray());

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

        // Save track data to file for future loading
        SaveTrackDataToFile(results);

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

    public static List<System.Numerics.Vector2> GetCurrentRaceline()
    {
        return currentRaceline;
    }

    public static PolygonTrack GetPolygonTrack()
    {
        return track;
    }

    private static void SaveTrackDataToFile(TrackImageProcessor.ProcessingResults results)
    {
        string filePath = Path.Combine(Application.persistentDataPath, instance.trackDataFileName);
        
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write raceline data
                writer.WriteLine("RACELINE");
                writer.WriteLine(results.raceline.Count);
                foreach (var point in results.raceline)
                {
                    writer.WriteLine($"{point.x},{point.y}");
                }

                // Write inner boundary data (note: results.outerBoundary is actually inner due to swap)
                writer.WriteLine("INNER_BOUNDARY");
                writer.WriteLine(results.outerBoundary.Count);
                foreach (var point in results.outerBoundary)
                {
                    writer.WriteLine($"{point.x},{point.y}");
                }

                // Write outer boundary data (note: results.innerBoundary is actually outer due to swap)
                writer.WriteLine("OUTER_BOUNDARY");
                writer.WriteLine(results.innerBoundary.Count);
                foreach (var point in results.innerBoundary)
                {
                    writer.WriteLine($"{point.x},{point.y}");
                }
            }
            
            Debug.Log($"Track data saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save track data: {e.Message}");
        }
    }

    private static TrackImageProcessor.ProcessingResults LoadTrackDataFromFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, instance.trackDataFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Track data file not found: {filePath}");
            return null;
        }

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                var results = new TrackImageProcessor.ProcessingResults();
                results.raceline = new List<Vector2>();
                results.innerBoundary = new List<Vector2>();
                results.outerBoundary = new List<Vector2>();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "RACELINE")
                    {
                        int count = int.Parse(reader.ReadLine());
                        for (int i = 0; i < count; i++)
                        {
                            string[] coords = reader.ReadLine().Split(',');
                            float x = float.Parse(coords[0]);
                            float y = float.Parse(coords[1]);
                            results.raceline.Add(new Vector2(x, y));
                        }
                    }
                    else if (line == "INNER_BOUNDARY")
                    {
                        int count = int.Parse(reader.ReadLine());
                        for (int i = 0; i < count; i++)
                        {
                            string[] coords = reader.ReadLine().Split(',');
                            float x = float.Parse(coords[0]);
                            float y = float.Parse(coords[1]);
                            // Note: saving to outerBoundary because of the swap in LoadTrack
                            results.outerBoundary.Add(new Vector2(x, y));
                        }
                    }
                    else if (line == "OUTER_BOUNDARY")
                    {
                        int count = int.Parse(reader.ReadLine());
                        for (int i = 0; i < count; i++)
                        {
                            string[] coords = reader.ReadLine().Split(',');
                            float x = float.Parse(coords[0]);
                            float y = float.Parse(coords[1]);
                            // Note: saving to innerBoundary because of the swap in LoadTrack
                            results.innerBoundary.Add(new Vector2(x, y));
                        }
                    }
                }
                
                Debug.Log($"Track data loaded from: {filePath}");
                return results;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load track data: {e.Message}");
            return null;
        }
    }
}
