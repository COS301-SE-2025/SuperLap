using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;

namespace RacelineOptimizer
{
    class Program
    {
        static void Main(string[] args)
        {
            //Use EdgeDataImporter to load edge data
            string edgeDataFilePath = "Input/edge_data.bin"; // Replace with actual path
            EdgeData edgeData = EdgeData.LoadFromBinary(edgeDataFilePath);
            if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
            {
                Console.WriteLine("Error: Edge data is empty or not loaded correctly.");
                return;
            }
            // Sample the track using TrackSampler
            int numSamples = 200; // Adjust as needed
            List<(Vector2 inner, Vector2 outer)> track = TrackSampler.Sample(edgeData.InnerBoundary, edgeData.OuterBoundary, numSamples);

            // Initialize PSO optimizer
            PSO pso = new PSO();
            int numParticles = 50; // Number of particles in the swarm
            int iterations = 200; // Number of iterations for optimization
            float[] bestRatios = pso.Optimize(track, numParticles, iterations);
            if (bestRatios == null || bestRatios.Length == 0)
            {
                Console.WriteLine("Error: Optimization failed to find a valid solution.");
                return;
            }
            // Generate the optimized raceline
            List<Vector2> raceline = pso.GenerateRaceline(track, bestRatios);

            // Save the raceline to a binary file
            string racelineFilePath = "Output/raceline.bin"; // Replace with actual path
            RacelineExporter.SaveToBinary(racelineFilePath, edgeData.InnerBoundary, edgeData.OuterBoundary, raceline);
            Console.WriteLine("Raceline optimization completed and saved to " + racelineFilePath);

        }
    }
}