using System.Numerics;
using System.Drawing;
using System.IO;

namespace RacelineOptimizer
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDir = "Input";
            string[] binFiles = Directory.GetFiles(inputDir, "*.bin");

            if (binFiles.Length == 0)
            {
                Console.WriteLine("No .bin files found in the Input directory.");
                return;
            }

            Console.WriteLine("Select a .bin file to process:");
            for (int i = 0; i < binFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}: {Path.GetFileName(binFiles[i])}");
            }
            Console.WriteLine($"{binFiles.Length + 1}: Run all files");

            Console.Write("Enter your choice (1 to " + (binFiles.Length + 1) + "): ");
            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > binFiles.Length + 1)
            {
                Console.WriteLine("Invalid choice.");
                return;
            }

            if (choice == binFiles.Length + 1)
            {
                foreach (string filePath in binFiles)
                {
                    ProcessFile(filePath);
                }
            }
            else
            {
                ProcessFile(binFiles[choice - 1]);
            }
        }

        static void ProcessFile(string edgeDataFilePath)
        {
            Console.WriteLine($"\nProcessing {Path.GetFileName(edgeDataFilePath)}...");

            EdgeData edgeData = EdgeData.LoadFromBinary(edgeDataFilePath);
            if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
            {
                Console.WriteLine("Error: Edge data is empty or not loaded correctly.");
                return;
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
                Console.WriteLine("Error: No corners detected in the track.");
                return;
            }

            PSO pso = new PSO();
            int numParticles = 100;
            int iterations = 60000;

            float[] bestRatios = pso.Optimize(track, corners, numParticles, iterations);
            if (bestRatios == null || bestRatios.Length == 0)
            {
                Console.WriteLine("Error: Optimization failed to find a valid solution.");
                return;
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
                true
            );
        }
    }
}
