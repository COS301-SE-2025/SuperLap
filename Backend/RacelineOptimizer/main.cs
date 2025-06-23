using System;
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
                    PSOInterface.Run(filePath, outputPath: "Output");
                }
            }
            else
            {
                PSOInterface.Run(binFiles[choice - 1], outputPath: "Output");
            }
        }
    }
}
