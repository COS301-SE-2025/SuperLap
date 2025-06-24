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
        public static bool Run(string edgeDataFilePath, string outputPath, int numParticles = 100, int iterations = 60000)
        {
            Debug.Log($"\nProcessing {Path.GetFileName(edgeDataFilePath)}...");

            EdgeData edgeData = EdgeData.LoadFromBinary(edgeDataFilePath);
            if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
            {
                Debug.Log("Error: Edge data is empty or not loaded correctly.");
                return false;
            }

            float avgWidth = edgeData.GetAverageTrackWidth();
            float scaleFactor = 125 / avgWidth;
            Vector2 center = edgeData.GetCenter();
            edgeData.ScaleTrack(center, scaleFactor);

            int numSamples = edgeData.OuterBoundary.Count < 500 || edgeData.InnerBoundary.Count < 500
                ? 700
                : (int)(edgeData.InnerBoundary.Count - (500 * scaleFactor));

            var track = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, numSamples);
            var cornerTrack = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, edgeData.InnerBoundary.Count);
            var corners = CornerDetector.DetectCorners(cornerTrack);

            if (corners.Count == 0)
            {
                Debug.Log("Error: No corners detected in the track.");
                return false;
            }

            PSO pso = new PSO();
            float[] bestRatios = pso.Optimize(track, corners, numParticles, iterations);
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
            RacelineExporter.SaveToBinary(racelineFilePath, edgeData.InnerBoundary, edgeData.OuterBoundary, raceline);
            Debug.Log("Raceline optimization completed and saved to " + racelineFilePath);

            return true;
        }

        public class RacelineResult
        {
            public List<Vector2> InnerBoundary { get; set; }
            public List<Vector2> OuterBoundary { get; set; }
            public List<Vector2> Raceline { get; set; }
        }

        public static RacelineResult GetRaceline(List<Vector2> innerBoundary, List<Vector2> outerBoundary, string trackName = "track", int numParticles = 100, int iterations = 60000)
        {
            Debug.Log($"Processing track: {trackName}...");

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

            float avgWidth = edgeData.GetAverageTrackWidth();
            float scaleFactor = 125 / avgWidth;
            Vector2 center = edgeData.GetCenter();

            Debug.Log($"Average track width: {avgWidth}, Scale factor: {scaleFactor}, Center: {center}");

            edgeData.ScaleTrack(center, scaleFactor);

            Debug.Log($"Scaled track with center: {center}, Scale factor: {scaleFactor}");

            int numSamples = edgeData.OuterBoundary.Count < 500 || edgeData.InnerBoundary.Count < 500
                ? 700
                : Math.Max(100, (int)(edgeData.InnerBoundary.Count - (500 * scaleFactor)));

            Debug.Log($"Calculated numSamples: {numSamples}");

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

            Debug.Log($"Detected {corners.Count} corners in the track.");

            if (corners.Count == 0)
            {
                Debug.Log("Error: No corners detected in the track.");
                return null;
            }

            PSO pso = new PSO();
            float[] bestRatios = pso.Optimize(track, corners, numParticles, iterations);
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

            innerBoundary = edgeData.InnerBoundary;
            outerBoundary = edgeData.OuterBoundary;

            RacelineResult result = new RacelineResult
            {
                InnerBoundary = innerBoundary,
                OuterBoundary = outerBoundary,
                Raceline = raceline
            };

            return result;
        }
    }
}
