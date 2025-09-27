using System.Numerics;
using System.Drawing;
using System.IO;

namespace RacelineOptimizer
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("What would you like to do?");
      Console.WriteLine("1: Convert CSV to BIN");
      Console.WriteLine("2: Run PSO on BIN files");
      Console.Write("Enter your choice (1 or 2): ");
      if (!int.TryParse(Console.ReadLine(), out int ch) || ch == 1)
      {
        string inputDir = "CSVInput";
        if (!Directory.Exists(inputDir))
        {
          Console.WriteLine("CSVInput directory does not exist. Please create it and add .csv files.");
          return;
        }
        string[] csvFiles = Directory.GetFiles(inputDir, "*.csv");
        if (csvFiles.Length == 0)
        {
          Console.WriteLine("No .csv files found in the CSVInput directory.");
          return;
        }
        Console.WriteLine("Select a .csv file to convert:");
        for (int i = 0; i < csvFiles.Length; i++)
        {
          Console.WriteLine($"{i + 1}: {Path.GetFileName(csvFiles[i])}");
        }
        Console.WriteLine($"{csvFiles.Length + 1}: Convert all files");
        Console.Write("Enter your choice (1 to " + (csvFiles.Length + 1) + "): ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > csvFiles.Length + 1)
        {
          Console.WriteLine("Invalid choice.");
          return;
        }
        if (choice == csvFiles.Length + 1)
        {
          foreach (string filePath in csvFiles)
          {
            if (!Directory.Exists("Output/CSV"))
            {
              Directory.CreateDirectory("Output/CSV");
            }
            string outputPath = Path.Combine("Output/CSV/", Path.GetFileNameWithoutExtension(filePath) + ".bin");
            CSVToBinConverter.LoadCSV.Convert(filePath, outputPath);
          }
        }
        else
        {
          if (!Directory.Exists("Output/CSV"))
          {
            Directory.CreateDirectory("Output/CSV");
          }
          string selectedFile = csvFiles[choice - 1];
          string outputPath = Path.Combine("Output/CSV/", Path.GetFileNameWithoutExtension(selectedFile) + ".bin");
          CSVToBinConverter.LoadCSV.Convert(selectedFile, outputPath);
        }
        return;
      }
      else
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
            string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            PSOInterface.Run(filePath, outputPath: "Output/" + fileNameNoExt);
            string outputDir = $"Output/{fileNameNoExt}";
            string racelineFilePath = $"{outputDir}/{fileNameNoExt}.bin";
            DebugImage(racelineFilePath, outputDir, fileNameNoExt);
          }
        }
        else
        {
          string fileNameNoExt = Path.GetFileNameWithoutExtension(binFiles[choice - 1]);
          PSOInterface.Run(binFiles[choice - 1], outputPath: "Output/" + fileNameNoExt);
          string outputDir = $"Output/{fileNameNoExt}";
          string racelineFilePath = $"{outputDir}/{fileNameNoExt}.bin";
          DebugImage(racelineFilePath, outputDir, fileNameNoExt);
        }
        Console.WriteLine("Processing complete. Check the Output directory for results.");
      }
    }

    static void DebugImage(string binPath, string outputDir, string fileNameNoExt)
    {
      RacelineVisualizer.EdgeDataVisualizer.DrawEdgesToImage(
        binPath: binPath,
        outputPath: $"./{outputDir}/{fileNameNoExt}.png",
        canvasSize: new Size(1920, 1080),
        includeRaceline: true,
        includeCorners: false
      );
    }
  }
}
