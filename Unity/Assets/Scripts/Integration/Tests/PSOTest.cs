using System.Collections.Generic;
using System.Linq;
using RacelineOptimizer;
using UnityEngine;

public class PSOTest : MonoBehaviour
{
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  [SerializeField]
  private LineRenderer innerBoundaryLineRenderer;
  [SerializeField]
  private LineRenderer outerBoundaryLineRenderer;
  [SerializeField]
  private LineRenderer racelineLineRenderer;
  void Start()
  {
    Debug.Log("Starting PSO Test...");
    ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(Application.dataPath.Replace("Unity/Assets", "Backend/ImageProcessing/trackImages/test.png"));
    Debug.Log($"Processing result: Success = {boundaries.success}, Error = {boundaries.errorMessage}");
    PSOInterface.RacelineResult racelineResult = PSOIntegrator.RunPSO(boundaries.innerBoundary, boundaries.outerBoundary, 100, 60000);

    // Convert System.Numerics.Vector2 to UnityEngine.Vector2
    List<Vector2> innerBoundary = ConvertToUnityVectors(racelineResult.InnerBoundary);
    List<Vector2> outerBoundary = ConvertToUnityVectors(racelineResult.OuterBoundary);
    List<Vector2> raceline = ConvertToUnityVectors(racelineResult.Raceline);

    DrawLines(innerBoundary, outerBoundary, raceline);

    Debug.Log("PSO Test completed.");
  }

  // Helper method to convert System.Numerics.Vector2 to UnityEngine.Vector2
  private List<Vector2> ConvertToUnityVectors(List<System.Numerics.Vector2> numericsVectors)
  {
    if (numericsVectors == null)
      return new List<Vector2>();

    return numericsVectors.Select(v => new Vector2(v.X, v.Y)).ToList();
  }

  void DrawLines(List<Vector2> innerBoundary, List<Vector2> outerBoundary, List<Vector2> raceline)
  {
    if (innerBoundary != null && innerBoundary.Count > 0)
    {
      innerBoundaryLineRenderer.positionCount = innerBoundary.Count;
      // Filter out every second point to avoid drawing the inner boundary twice
      // innerBoundary = innerBoundary.Where((_, index) => index % 2 == 0).ToList();
      // innerBoundaryLineRenderer.positionCount = innerBoundary.Count;
      innerBoundaryLineRenderer.SetPositions(innerBoundary.ConvertAll(v => new Vector3(v.x, v.y, 0)).ToArray());
    }
    else
    {
      Debug.LogWarning("Inner boundary is empty or null.");
    }

    if (outerBoundary != null && outerBoundary.Count > 0)
    {
      outerBoundaryLineRenderer.positionCount = outerBoundary.Count;
      // Filter out every second point to avoid drawing the outer boundary twice
      // outerBoundary = outerBoundary.Where((_, index) => index % 2 == 0).ToList();
      // outerBoundaryLineRenderer.positionCount = outerBoundary.Count;
      outerBoundaryLineRenderer.SetPositions(outerBoundary.ConvertAll(v => new Vector3(v.x, v.y, 0)).ToArray());
    }
    else
    {
      Debug.LogWarning("Outer boundary is empty or null.");
    }

    if (raceline != null && raceline.Count > 0)
    {
      racelineLineRenderer.positionCount = raceline.Count;
      racelineLineRenderer.SetPositions(raceline.ConvertAll(v => new Vector3(v.x, v.y, 0)).ToArray());
    }
    else
    {
      Debug.LogWarning("Raceline is empty or null.");
    }

  }
}
