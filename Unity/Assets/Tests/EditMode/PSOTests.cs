using NUnit.Framework;
using System.IO;
using UnityEngine;
using RacelineOptimizer;

public class PSOFileTests
{
    private string GetTestBinFilePath()
    {
        // Assuming your .bin test file is here
        return Path.Combine(Application.dataPath, "Tests/Resources/test_track.bin");
    }

    private string GetOutputFolderPath()
    {
        return Path.Combine(Application.dataPath, "Tests/Output");
    }

    [Test]
    public void RunPSO_WithValidBinFile_CompletesSuccessfully()
    {
        string binFile = GetTestBinFilePath();
        string outputDir = GetOutputFolderPath();

        // Make sure output directory exists
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        // Run PSOInterface with test .bin file
        bool success = PSOInterface.Run(binFile, outputDir, numParticles: 30, iterations: 200);

        Assert.IsTrue(success, "PSOInterface.Run() should return true on valid input.");

        // Verify output raceline file is created (same name, in output folder)
        string outputRacelineFile = Path.Combine(outputDir, Path.GetFileName(binFile));
        Assert.IsTrue(File.Exists(outputRacelineFile), "Output raceline .bin file not found.");
    }
}
