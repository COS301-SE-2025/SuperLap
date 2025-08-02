using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;

namespace RacelineOptimizer
{
    public static class PSOInterface
    {
        public static bool Run(string edgeDataFilePath, string outputPath, int numParticles = 100, int iterations = 10000)
        {
            Console.WriteLine($"\nProcessing {Path.GetFileName(edgeDataFilePath)}...");

            EdgeData edgeData = EdgeData.LoadFromBinary(edgeDataFilePath);
            if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
            {
                Console.WriteLine("Error: Edge data is empty or not loaded correctly.");
                return false;
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
                Console.WriteLine("Error: No corners detected in the track.");
                return false;
            }

            PSO pso = new PSO();
            float[] bestRatios = pso.Optimize(track, corners, cornerTrack, numParticles, iterations);
            if (bestRatios == null || bestRatios.Length == 0)
            {
                Console.WriteLine("Error: Optimization failed to find a valid solution.");
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
            Console.WriteLine($"Exporting {corners.Count} corners");

            RacelineExporter.SaveToBinary(racelineFilePath, edgeData.InnerBoundary, edgeData.OuterBoundary, raceline, corners);
            Console.WriteLine("Raceline optimization completed and saved to " + racelineFilePath);

            return true;
        }
    }
}
