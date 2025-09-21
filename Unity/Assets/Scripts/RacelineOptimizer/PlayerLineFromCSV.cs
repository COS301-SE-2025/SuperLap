using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = System.Numerics.Vector2;

namespace CSVToBinConverter
{
  public static class LoadCSV
  {
    public class PlayerLine
    {
      public List<Vector2> InnerBoundary { get; set; }
      public List<Vector2> OuterBoundary { get; set; }
      public List<Vector2> Raceline { get; set; }
      public List<Vector2> PlayerPath { get; set; }
      public List<Vector2> WorstDeviationSections { get; set; } = new();
    }

    public static PlayerLine Convert(string csvPath, int targetLapIndex = 0)
    {
      List<Vector2> playerline = new();
      using var reader = new StreamReader(csvPath);
      string? headerLine = reader.ReadLine();
      if (headerLine == null)
        throw new Exception("CSV is empty");

      string[] headers = headerLine.Split('\t');
      int lapIdxIndex = Array.IndexOf(headers, "lap_number");
      int xIdx = Array.IndexOf(headers, "world_position_X");
      int yIdx = Array.IndexOf(headers, "world_position_Y");
      int trackIndex = Array.IndexOf(headers, "trackId");
      string trackName = "Unknown";



      if (lapIdxIndex == -1 || xIdx == -1 || yIdx == -1)
        throw new Exception("Required columns not found in CSV");

      List<(float x, float y)> rawPoints = new();

      while (!reader.EndOfStream)
      {
        string? line = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) continue;

        string[] fields = line.Split('\t');
        if (fields.Length <= Math.Max(lapIdxIndex, Math.Max(xIdx, yIdx))) continue;

        if (trackIndex != -1 && fields.Length > trackIndex)
          trackName = fields[trackIndex];

        if (!int.TryParse(fields[lapIdxIndex], out int lapIndex) || lapIndex != targetLapIndex)
          continue;

        if (float.TryParse(fields[xIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(fields[yIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
          rawPoints.Add((x, y));
        }
      }

      string settingsPath = Path.Combine(Application.streamingAssetsPath, "AdjustPlayerlineSettings", $"{trackName}.json");
      PlayerlineSettings settings = LoadSettings(settingsPath) ?? new PlayerlineSettings
      {
        tx = 0f,
        ty = 0f,
        scale = 1f,
        rotation = 0f,
        reflect_x = false,
        reflect_y = false
      };

      float rad = settings.rotation * (float)Math.PI / 180f;
      float cos = MathF.Cos(rad);
      float sin = MathF.Sin(rad);
      foreach (var (x, y) in rawPoints)
      {
        float rotatedX = cos * x - sin * y;
        float rotatedY = sin * x + cos * y;

        rotatedX *= settings.scale;
        rotatedY *= settings.scale;

        if (settings.reflect_x) rotatedX = -rotatedX;
        if (settings.reflect_y) rotatedY = -rotatedY;

        rotatedX += settings.tx;
        rotatedY += settings.ty;

        playerline.Add(new Vector2(rotatedX, rotatedY));
      }

      if (playerline.Count == 0)
      {
        Debug.Log("No valid playerline data found.");
        return null;
      }

      string trackPath = Path.Combine(Application.streamingAssetsPath, "MotoGPTracks", $"{trackName}.bin");
      if (!File.Exists(trackPath))
      {
        Debug.Log($"Error: Edge file not found at {trackPath}");
        return null;
      }

      EdgeData edgeData = EdgeData.LoadFromBinary(trackPath, true);

      if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
      {
        Debug.Log($"Error: Edge data for {trackName} is empty or malformed.");
        return null;
      }
      List<Vector2> worstSections = DeviationAnalyzer.GetWorstDeviationSections(playerline, edgeData.Raceline, 5);
      Debug.Log($"Found {worstSections.Count} worst deviation sections.");
      return new PlayerLine
      {
        InnerBoundary = edgeData.InnerBoundary,
        OuterBoundary = edgeData.OuterBoundary,
        Raceline = edgeData.Raceline,
        PlayerPath = playerline,
        WorstDeviationSections = worstSections
      };
    }

    private static PlayerlineSettings LoadSettings(string path)
    {
      try
      {
        if (!File.Exists(path))
          return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayerlineSettings>(json);
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to load playerline settings from {path}: {ex.Message}");
        return null;
      }
    }

    public static void SaveToBin(PlayerLine data, string binOutputPath)
    {
      if (data == null)
        throw new ArgumentNullException(nameof(data));

      string fileNameNoExt = Path.GetFileNameWithoutExtension(binOutputPath);
      string outputDir = Path.GetDirectoryName(binOutputPath);

      if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        Directory.CreateDirectory(outputDir);

      using (var writer = new BinaryWriter(File.Create(binOutputPath)))
      {
        WritePoints(writer, data.OuterBoundary);
        WritePoints(writer, data.InnerBoundary);
        WritePoints(writer, data.Raceline);
        WritePoints(writer, data.PlayerPath);
      }

      Debug.Log($"Combined edge + playerline data written to: {binOutputPath}");
    }


    private static void WritePoints(BinaryWriter writer, List<Vector2> points)
    {
      writer.Write(points.Count);
      foreach (var pt in points)
      {
        writer.Write(pt.X);
        writer.Write(pt.Y);
      }
    }
  }
}
