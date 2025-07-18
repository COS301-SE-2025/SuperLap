using System.Collections.Generic;
using UnityEngine;

public class TrackMaster : MonoBehaviour
{
    [Header("Track Master Settings")]
    [SerializeField] private int meshResolution = 1000;
    [SerializeField] private int splitCount = 50;
    [SerializeField] private float splitMeshScale = 25f;


    public static TrackMaster instance;

    void Start()
    {
        instance = this;
    }

    void Update()
    {

    }

    public static void LoadTrack(TrackImageProcessor.ProcessingResults results)
    {
        Mesh mesh = Processor3D.GenerateOutputMesh(results, instance.meshResolution);
        MeshFilter meshFilter = instance.gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = instance.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material.color = Color.white;
        instance.GetComponent<MeshFilter>().mesh = mesh;

        CreateSplits(results.raceline);
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
            splitPoint.transform.SetParent(instance.transform); // Set parent to keep hierarchy clean
            splitPoint.name = $"SplitPoint_{i}";
        }
    }
}
