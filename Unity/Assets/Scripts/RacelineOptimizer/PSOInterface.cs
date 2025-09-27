using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;
using Vec2 = UnityEngine.Vector2;

namespace RacelineOptimizer
{
  public static class PSOInterface
  {
    public static bool Run(string edgeDataFilePath, string outputPath, int numParticles = 100, int iterations = 6000, bool enableBranchDetection = false)
    {
      Debug.Log($"\nProcessing {Path.GetFileName(edgeDataFilePath)}...");
      EdgeData edgeData = EdgeData.LoadFromBinary(edgeDataFilePath);
      if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
      {
        Debug.Log("Error: Edge data is empty or not loaded correctly.");
        return false;
      }

      // Apply branch detection and mitigation
      if (enableBranchDetection)
      {
        Debug.Log("Analyzing track boundaries for potential branches...");
        BranchDetector.AnalyzeTrackCharacteristics(edgeData.InnerBoundary, edgeData.OuterBoundary);
        
        var branchConfig = new BranchDetector.BranchDetectionConfig
        {
          SpikeThreshold = 2.0f,    // Track sections 2x thicker than average
          MinSpikeRatio = 0.05f,    // Minimum 5% of track length
          MaxSpikeRatio = 0.3f,     // Maximum 30% of track length
          SmoothingWindow = 5,
          DerivativeThreshold = 1.5f
        };
        
        edgeData.ProcessBranches(branchConfig);
        Debug.Log("Branch detection and mitigation completed.");
      }

      float avgWidth = edgeData.GetAverageTrackWidth();
      float scaleFactor = 125 / avgWidth;
      Vector2 center = edgeData.GetCenter();
      edgeData.ScaleTrack(center, scaleFactor);

      var track = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, 400);
      var cornerTrack = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, edgeData.InnerBoundary.Count);
      var corners = CornerDetector.DetectCorners(cornerTrack);

      if (corners.Count == 0)
      {
        Debug.Log("Error: No corners detected in the track.");
        return false;
      }

      PSO pso = new PSO();
      float[] bestRatios = pso.Optimize(track, corners, cornerTrack, numParticles, iterations);
      if (bestRatios == null || bestRatios.Length == 0)
      {
        Debug.Log("Error: Optimization failed to find a valid solution.");
        return false;
      }

      var raceline = new List<Vector2>();
      for (int i = 0; i < track.Count; i++)
      {
        raceline.Add(Vector2.Lerp(track[i].inner, track[i].outer, bestRatios[i]));
      }
      raceline = pso.SmoothRaceline(raceline, iterations: 2);

      int sgWindowSize = 7;
      int sgPolyOrder = 3;
      var sgFilter = new SavitzkyGolayFilter(sgWindowSize, sgPolyOrder);

      List<float> xCoords = raceline.Select(p => p.X).ToList();
      List<float> yCoords = raceline.Select(p => p.Y).ToList();
      List<float> smoothedX = sgFilter.Smooth(xCoords);
      List<float> smoothedY = sgFilter.Smooth(yCoords);

      for (int i = 0; i < raceline.Count; i++)
      {
        raceline[i] = new Vector2(smoothedX[i], smoothedY[i]);
      }

      string fileNameNoExt = Path.GetFileNameWithoutExtension(edgeDataFilePath);
      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }

      string racelineFilePath = $"{outputPath}/{fileNameNoExt}.bin";
      Debug.Log($"Exporting {corners.Count} corners");

      RacelineExporter.SaveToBinary(racelineFilePath, edgeData.InnerBoundary, edgeData.OuterBoundary, raceline, corners);
      Debug.Log("Raceline optimization completed and saved to " + racelineFilePath);

      return true;
    }

    public class RacelineResult
    {
      public List<Vector2> InnerBoundary { get; set; }
      public List<Vector2> OuterBoundary { get; set; }
      public List<Vector2> Raceline { get; set; }
      public List<Vector2> BreakPoints { get; set; }
    }
    

    public static RacelineResult GetRaceline(
    List<Vector2> innerBoundary,
    List<Vector2> outerBoundary,
    string trackName = "track",
    int numParticles = 100,
    int iterations = 6000,
    string outputPath = "Output",
    bool enableBranchDetection = false,
    BranchDetector.BranchDetectionConfig branchConfig = null)
    {
      Debug.Log($"Processing track: {trackName}...");
      Debug.Log($"Running PSO with {numParticles} particles and {iterations} iterations");

      if (outerBoundary.Count == 0 || innerBoundary.Count == 0)
      {
        Debug.Log("Error: Boundary data is empty.");
        return null;
      }
      else
      {
        Debug.Log($"Outer boundary points: {outerBoundary.Count}, Inner boundary points: {innerBoundary.Count}");
      }

      // Create EdgeData object from the provided vectors
      EdgeData edgeData = EdgeData.LoadFromLists(innerBoundary, outerBoundary);

      // Apply branch detection and mitigation before any processing
      if (enableBranchDetection)
      {
        Debug.Log("Analyzing track boundaries for potential branches...");
        BranchDetector.AnalyzeTrackCharacteristics(edgeData.InnerBoundary, edgeData.OuterBoundary);

        // Use provided config or create default
        branchConfig ??= new BranchDetector.BranchDetectionConfig
        {
          SpikeThreshold = 2.0f,    // Track sections 2x thicker than average
          MinSpikeRatio = 0.05f,    // Minimum 5% of track length
          MaxSpikeRatio = 0.3f,     // Maximum 30% of track length
          SmoothingWindow = 5,
          DerivativeThreshold = 1.5f
        };

        edgeData.ProcessBranches(branchConfig);
        Debug.Log("Branch detection and mitigation completed.");
      }

      float avgWidth = edgeData.GetAverageTrackWidth();
      float scaleFactor = 125 / avgWidth;
      Vector2 center = edgeData.GetCenter();

      edgeData.ScaleTrack(center, scaleFactor);

      int numSamples = 400;

      var track = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, numSamples);
      if (track.Count == 0)
      {
        Debug.Log("Error: Track sampling returned no points.");
        return null;
      }

      var cornerTrack = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, edgeData.InnerBoundary.Count);
      if (cornerTrack.Count == 0)
      {
        Debug.Log("Error: Corner track sampling returned no points.");
        return null;
      }

      var corners = CornerDetector.DetectCorners(cornerTrack);
      if (corners.Count == 0)
      {
        Debug.Log("Error: No corners detected in the corner track.");
        return null;
      }

      PSO pso = new PSO();
      float[] bestRatios = pso.Optimize(track, corners, cornerTrack, numParticles, iterations);
      if (bestRatios == null || bestRatios.Length == 0)
      {
        Debug.Log("Error: Optimization failed to find a valid solution.");
        return null;
      }

      var raceline = new List<Vector2>();
      for (int i = 0; i < track.Count; i++)
      {
        raceline.Add(Vector2.Lerp(track[i].inner, track[i].outer, bestRatios[i]));
      }
      raceline = pso.SmoothRaceline(raceline, iterations: 2);

      // Apply Savitzky-Golay smoothing
      int sgWindowSize = 7;
      int sgPolyOrder = 3;
      var sgFilter = new SavitzkyGolayFilter(sgWindowSize, sgPolyOrder);

      List<float> xCoords = raceline.Select(p => p.X).ToList();
      List<float> yCoords = raceline.Select(p => p.Y).ToList();
      List<float> smoothedX = sgFilter.Smooth(xCoords);
      List<float> smoothedY = sgFilter.Smooth(yCoords);

      for (int i = 0; i < raceline.Count; i++)
      {
        raceline[i] = new Vector2(smoothedX[i], smoothedY[i]);
      }

      // Update boundaries after scaling
      innerBoundary = edgeData.InnerBoundary;
      outerBoundary = edgeData.OuterBoundary;

      // Save to bin file
      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }
      string racelineFilePath = Path.Combine(outputPath, $"{trackName}.bin");
      RacelineExporter.SaveToBinary(racelineFilePath, innerBoundary, outerBoundary, raceline, corners);
      Debug.Log($"Raceline optimization completed and saved to {racelineFilePath}");


      // Helper local functions (put these near the method, or static helpers in the same class)
      int MapIndexToRaceline(int idx, int sourceCount, int targetCount)
      {
        if (sourceCount <= 0 || targetCount <= 0) return 0;
        int mapped = (int)((float)idx / (float)sourceCount * targetCount);
        return Mathf.Clamp(mapped, 0, targetCount - 1);
      }

      float ComputeSegmentLength(List<Vector2> pts, int startIndex, int endIndex)
      {
        if (pts == null || pts.Count < 2) return 0f;
        int n = pts.Count;
        float length = 0f;
        int i = startIndex;
        while (true)
        {
          int next = (i + 1) % n;
          length += Vector2.Distance(pts[i], pts[next]);
          if (i == endIndex) break;
          i = next;
          // if we wrapped all the way round, stop
          if (i == startIndex) break;
        }
        return length;
      }

      Vector2 GetPointAlongRaceline(List<Vector2> raceline, int startIndex, int endIndex, float fraction)
      {
          if (raceline == null || raceline.Count < 2) return new Vector2(0, 0);

          int n = raceline.Count;
          float totalDist = 0f;

          // compute total segment distance from start → end
          int i = startIndex;
          while (i != endIndex)
          {
              int next = (i + 1) % n;
              totalDist += Vector2.Distance(raceline[i], raceline[next]);
              i = next;
          }

          float targetDist = totalDist * Mathf.Clamp01(fraction);
          float traveled = 0f;
          i = startIndex;

          // walk until we reach targetDist
          while (i != endIndex)
          {
              int next = (i + 1) % n;
              float segLen = Vector2.Distance(raceline[i], raceline[next]);
              if (traveled + segLen >= targetDist)
              {
                  float t = (targetDist - traveled) / segLen;
                  return Vector2.Lerp(raceline[i], raceline[next], t);
              }
              traveled += segLen;
              i = next;
          }

          return raceline[endIndex]; // fallback = end point
      }


      // Sum signed turning angles (in degrees) at interior points between startIndex and endIndex.
    // Uses the segment angle between (p[k]-p[k-1]) and (p[k+1]-p[k]).
    List<UnityEngine.Vector2> ConvertToUnityVector2(List<System.Numerics.Vector2> list)
      {
        return list.Select(v => new UnityEngine.Vector2(v.X, v.Y)).ToList();
      }

      float ComputeTotalSignedAngleAlong(List<Vector2> pts, int startIndex, int endIndex)
      {
        List<UnityEngine.Vector2> uPts = ConvertToUnityVector2(pts);
        if (uPts == null || uPts.Count < 3) return 0f;
        int n = uPts.Count;

        // compute forward step count from start to end (handle wrap)
        int steps = endIndex >= startIndex ? endIndex - startIndex : (n - startIndex + endIndex);

        // need at least two segments to have an interior turning angle
        if (steps < 2) return 0f;

        float total = 0f;
        for (int s = 1; s < steps; s++)
        {
          int idx = (startIndex + s) % n;
          int prev = (idx - 1 + n) % n;
          int next = (idx + 1) % n;

          Vec2 v1 = (uPts[idx] - uPts[prev]).normalized;
          Vec2 v2 = (uPts[next] - uPts[idx]).normalized;



          float cross = v1.x * v2.y - v1.y * v2.x;
          float dot = Vec2.Dot(v1, v2);
          float ang = Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;
          total += ang;
        }
        return total;
      }

      var breakPoints = new List<Vector2>();

      // tune these for your units / desired behaviour
      float minBrake = 42.69f;    // minimum braking distance
      float maxBrake = 420f;  // maximum braking distance for the sharpest corners
      float angleNormalization = 120f; // degrees that map to ~1.0 (tune)
      float curvatureNormalization = 0.2f; // rad/m that maps to ~1.0 (tune)

      for (int ci = 0; ci < corners.Count; ci++)
      {
        var corner = corners[ci];

        // map corner indices (cornerTrack -> raceline)
        int mappedStart = MapIndexToRaceline(corner.StartIndex, cornerTrack.Count, raceline.Count);
        int mappedEnd = MapIndexToRaceline(corner.EndIndex, cornerTrack.Count, raceline.Count);

        // compute metrics on the raceline between mappedStart..mappedEnd (handles wrap)
        float totalAngleDeg = Mathf.Abs(ComputeTotalSignedAngleAlong(raceline, mappedStart, mappedEnd));
        float segmentLen = ComputeSegmentLength(raceline, mappedStart, mappedEnd);

        float totalAngleRad = totalAngleDeg * Mathf.Deg2Rad;
        float curvature = (segmentLen > 0f && totalAngleRad > 1e-6f) ? totalAngleRad / segmentLen : 0f;
        float estimatedRadius = (totalAngleRad > 1e-6f) ? (segmentLen / totalAngleRad) : float.MaxValue;

        // normalize metrics (clamp 0..1)
        float angleNorm = Mathf.Clamp01(totalAngleDeg / angleNormalization);
        float curvatureNorm = Mathf.Clamp01(curvature / curvatureNormalization);

        // pick a severity (you can weight these differently; here we take the max which is simple and robust)
        float severity = Mathf.Max(angleNorm, curvatureNorm);

        // map severity to braking distance
        float brakingDistance = Mathf.Lerp(minBrake, maxBrake, severity);

        // find braking start by walking backward along the raceline from mappedStart
        Vector2 cornerEntry = GetPointAlongRaceline(raceline, mappedStart, mappedEnd, 0.45f);
        Vector2 brakingStart = cornerEntry;
        float accumulated = 0f;
        int i = mappedStart;
        while (accumulated < brakingDistance)
        {
          int prev = (i - 1 + raceline.Count) % raceline.Count;
          float d = Vector2.Distance(raceline[i], raceline[prev]);
          accumulated += d;
          brakingStart = raceline[prev];
          i = prev;

          // safety: if we've looped full circle stop
          if (i == mappedEnd) break;
        }

        breakPoints.Add(brakingStart);
        breakPoints.Add(cornerEntry);

        Debug.Log($"Corner #{ci}: totalAngle={totalAngleDeg:F1}°, len={segmentLen:F1}, radius≈{estimatedRadius:F1}, severity={severity:F2}, brakeDist={brakingDistance:F1}");
      }

      Debug.Log($"Identified {breakPoints.Count} braking points.");
      RacelineResult result = new RacelineResult
      {
        InnerBoundary = innerBoundary,
        OuterBoundary = outerBoundary,
        Raceline = raceline,
        BreakPoints = breakPoints
      };

      return result;
    }

    
    

    // Utility method for testing branch detection settings
    public static void TestBranchDetection(List<Vector2> innerBoundary, List<Vector2> outerBoundary,
      BranchDetector.BranchDetectionConfig config = null)
    {
      Debug.Log("=== Branch Detection Test ===");
      var widths = BranchDetector.AnalyzeTrackCharacteristics(innerBoundary, outerBoundary);

      config ??= new BranchDetector.BranchDetectionConfig();
      var (processedInner, processedOuter) = BranchDetector.ProcessBoundaries(innerBoundary, outerBoundary, config);

      if (processedInner.Count != innerBoundary.Count || processedOuter.Count != outerBoundary.Count)
      {
        Debug.Log("Branch mitigation modified the boundaries.");
      }
      else
      {
        Debug.Log("No branches detected or boundaries unchanged.");
      }
      Debug.Log("=== End Branch Detection Test ===");
    }
  }
}