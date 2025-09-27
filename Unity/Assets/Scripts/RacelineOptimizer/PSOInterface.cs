using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace RacelineOptimizer
{
  public static class PSOInterface
  {
    public static bool Run(string edgeDataFilePath, string outputPath, int numParticles = 100, int iterations = 6000, bool enableBranchDetection = true)
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
    }

    public static RacelineResult GetRaceline(
    List<Vector2> innerBoundary,
    List<Vector2> outerBoundary,
    string trackName = "track",
    int numParticles = 100,
    int iterations = 6000,
    string outputPath = "Output",
    bool enableBranchDetection = true,
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

      RacelineResult result = new RacelineResult
      {
        InnerBoundary = innerBoundary,
        OuterBoundary = outerBoundary,
        Raceline = raceline
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