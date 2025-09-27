using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
    private TrackImageProcessor.ProcessingResults lastProcessingResults;
    public TrackImageProcessor.ProcessingResults LastProcessingResults => lastProcessingResults;

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

    public static List<Vector2> SimplifyLine(List<Vector2> points, float tolerance)
    {
        if (points == null || points.Count <= 2)
        {
            return points;
        }

        // Use Douglas-Peucker algorithm for line simplification
        return DouglasPeucker(points, tolerance);
    }

    private static List<Vector2> DouglasPeucker(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2)
        {
            return new List<Vector2>(points);
        }

        // Find the point with the maximum distance from the line between first and last points
        float maxDistance = 0;
        int maxIndex = 0;
        
        Vector2 lineStart = points[0];
        Vector2 lineEnd = points[points.Count - 1];
        
        for (int i = 1; i < points.Count - 1; i++)
        {
            float distance = PerpendicularDistance(points[i], lineStart, lineEnd);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        // If max distance is greater than tolerance, recursively simplify
        if (maxDistance > tolerance)
        {
            // Recursively simplify the two segments
            List<Vector2> firstHalf = new List<Vector2>();
            for (int i = 0; i <= maxIndex; i++)
            {
                firstHalf.Add(points[i]);
            }
            
            List<Vector2> secondHalf = new List<Vector2>();
            for (int i = maxIndex; i < points.Count; i++)
            {
                secondHalf.Add(points[i]);
            }

            List<Vector2> result1 = DouglasPeucker(firstHalf, tolerance);
            List<Vector2> result2 = DouglasPeucker(secondHalf, tolerance);

            // Combine results, avoiding duplicate point at the connection
            List<Vector2> result = new List<Vector2>(result1);
            for (int i = 1; i < result2.Count; i++) // Skip first point to avoid duplicate
            {
                result.Add(result2[i]);
            }
            
            return result;
        }
        else
        {
            // If no point is far enough, simplify to just start and end points
            return new List<Vector2> { points[0], points[points.Count - 1] };
        }
    }

    private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineVector = lineEnd - lineStart;
        Vector2 pointVector = point - lineStart;
        
        // Handle degenerate case where line has zero length
        if (lineVector.sqrMagnitude < 0.0001f)
        {
            return Vector2.Distance(point, lineStart);
        }
        
        // Calculate perpendicular distance using cross product
        float crossProduct = Mathf.Abs(lineVector.x * pointVector.y - lineVector.y * pointVector.x);
        float lineLength = lineVector.magnitude;
        
        return crossProduct / lineLength;
    }

    public static void LoadTrack(TrackImageProcessor.ProcessingResults results)
    {
        instance.lastProcessingResults = results;
        // Store raceline for agent spawning
        List<Vector2> rl = results.raceline;
        Debug.Log($"Original raceline has {rl.Count} points.");
        List<Vector2> simplifiedRaceline = SimplifyLine(rl, 1.0f);
        Debug.Log($"Simplified raceline has {simplifiedRaceline.Count} points.");
        List<System.Numerics.Vector2> vector2s = new List<System.Numerics.Vector2>();
        foreach (var vec in simplifiedRaceline)
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

        List<Vector2> temp = results.innerBoundary;
        List<Vector2> temp2 = results.outerBoundary;
        List<Vector2> simpleInner = SimplifyLine(temp, 2.0f);
        List<Vector2> simpleOuter = SimplifyLine(temp2, 2.0f);
        Debug.Log($"Simplified inner boundary to {simpleInner.Count} points and outer boundary to {simpleOuter.Count} points.");
        foreach (var vec in simpleOuter) // This is actually the inner boundary
        {
            inner.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }
        foreach (var vec in simpleInner) // This is actually the outer boundary
        {
            outer.Add(new System.Numerics.Vector2(vec.x, vec.y));
        }

        track = new PolygonTrack(outer.ToArray(), inner.ToArray());

        Mesh mesh = Processor3D.GenerateOutputMesh(innerPoints, outerPoints);
        MeshFilter meshFilter = instance.gameObject.AddComponent<MeshFilter>() ?? instance.gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = instance.gameObject.AddComponent<MeshRenderer>() ?? instance.gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material.color = Color.white;
        instance.GetComponent<MeshFilter>().mesh = mesh;

        // CreateCheckpoints(results);
        CreateRacelineVisualization(results.raceline);

        // Initialize the RacelineAnalyzer with the raceline data for optimized queries

        // Save track data to file for future loading
        if(instance.loadTestTrack)
        {
            SaveTrackDataToFile(results);
        }

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

    public static void CreateLineVisualization(List<Vector2> linePoints, string name, Color color, float width)
    {
        if (!instance.showRaceline || linePoints == null || linePoints.Count == 0)
        {
            if (racelineRenderer != null)
            {
                racelineRenderer.enabled = false;
            }
            return;
        }
        LineRenderer lineRenderer = null;
        // Create or get the LineRenderer component
        if (lineRenderer == null)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(instance.transform);
            lineRenderer = lineObject.AddComponent<LineRenderer>();
        }

        // Configure the LineRenderer
        if (lineRenderer == null) return;

        // Create a material for the line if it doesn't exist
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = color;

        // Configure LineRenderer properties
        lineRenderer.material = lineMat;
        lineRenderer.startWidth = instance.racelineWidth;
        lineRenderer.endWidth = instance.racelineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false; // We manually close the loop
        lineRenderer.sortingOrder = 1; // Render on top of track

        // Convert 2D raceline points to 3D positions with height offset
        Vector3[] linePoints3D = new Vector3[linePoints.Count + 1]; // +1 to close the loop

        for (int i = 0; i < linePoints.Count; i++)
        {
            Vector2 point = linePoints[i];
            linePoints3D[i] = new Vector3(point.x, instance.racelineHeightOffset, point.y);
        }

        // Close the loop by connecting back to the first point
        linePoints3D[linePoints.Count] = linePoints3D[0];

        // Apply the points to the LineRenderer
        lineRenderer.positionCount = linePoints3D.Length;
        lineRenderer.SetPositions(linePoints3D);
        lineRenderer.enabled = true;

        Debug.Log($"Created line visualization with {linePoints.Count} points");
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
    
    // Create a thread-safe copy of the track for worker threads
    public static PolygonTrack CreatePolygonTrackCopy()
    {
        if (track == null) return null;
        return new PolygonTrack(track);
    }
    
    // Create a thread-safe copy of the raceline for worker threads
    public static List<System.Numerics.Vector2> CreateRacelineCopy()
    {
        if (currentRaceline == null) return null;
        return new List<System.Numerics.Vector2>(currentRaceline);
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
