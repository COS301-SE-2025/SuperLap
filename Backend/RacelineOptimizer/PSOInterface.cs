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
        public static bool Run(string edgeDataFilePath, int numParticles = 100, int iterations = 60000)
        {
            Console.WriteLine($"\nProcessing {Path.GetFileName(edgeDataFilePath)}...");

            EdgeData edgeData = EdgeData.LoadFromBinary(edgeDataFilePath);

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

            PSO pso = new PSO();
            float[] bestRatios = pso.Optimize(track, corners, numParticles, iterations);

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
            string outputDir = $"Output/{fileNameNoExt}";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string racelineFilePath = $"{outputDir}/{fileNameNoExt}.bin";
            RacelineExporter.SaveToBinary(racelineFilePath, edgeData.InnerBoundary, edgeData.OuterBoundary, raceline);
            Console.WriteLine("Raceline optimization completed and saved to " + racelineFilePath);

            RacelineVisualizer.EdgeDataVisualizer.DrawEdgesToImage(
                binPath: racelineFilePath,
                outputPath: $"./{outputDir}/{fileNameNoExt}.png",
                canvasSize: new Size(1920, 1080),
                includeRaceline: true,
                drawCenter: true
            );

            return true;
        }
    }
}
