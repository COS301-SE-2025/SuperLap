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
          if (trackName == "Losail")
          {
            float angleDeg = -60f;
            float angleRad = angleDeg * (float)Math.PI / 180f;
            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);
            float rotatedX = cos * x - sin * y;
            float rotatedY = sin * x + cos * y;

            float scaleFactor = 6.834f; // Adjust scale factor as needed
            rotatedX *= scaleFactor;
            rotatedY *= scaleFactor;

            playerline.Add(new Vector2(rotatedX, rotatedY));
          }
          else
          {
            float angleDeg = 145.0f;
            float angleRad = angleDeg * (float)Math.PI / 180f;
            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);
            float rotatedX = cos * x - sin * y;
            float rotatedY = sin * x + cos * y;

            float scaleFactor = 0.514f; // Adjust scale factor as needed
            rotatedX *= scaleFactor;
            rotatedY *= scaleFactor;

            playerline.Add(new Vector2(-rotatedX, rotatedY));
          }
        }
      }

      if (playerline.Count == 0)
      {
        Debug.Log("No valid playerline data found.");
        return null;
      }

      // Load existing edge data
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

      var allEdges = edgeData.OuterBoundary.Concat(edgeData.InnerBoundary).ToList();

      float edgeMinX = allEdges.Min(p => p.X);
      float edgeMaxX = allEdges.Max(p => p.X);
      float edgeMinY = allEdges.Min(p => p.Y);
      float edgeMaxY = allEdges.Max(p => p.Y);

      float edgeCenterX = (edgeMinX + edgeMaxX) / 2f;
      float edgeCenterY = (edgeMinY + edgeMaxY) / 2f;

      for (int i = 0; i < edgeData.OuterBoundary.Count; i++)
      {
        edgeData.OuterBoundary[i] -= new Vector2(edgeCenterX, edgeCenterY);
      }

      for (int i = 0; i < edgeData.InnerBoundary.Count; i++)
      {
        edgeData.InnerBoundary[i] -= new Vector2(edgeCenterX, edgeCenterY);
      }

      for (int i = 0; i < edgeData.Raceline.Count; i++)
      {
        edgeData.Raceline[i] -= new Vector2(edgeCenterX, edgeCenterY);
      }

      float playerMinX = playerline.Min(p => p.X);
      float playerMaxX = playerline.Max(p => p.X);
      float playerMinY = playerline.Min(p => p.Y);
      float playerMaxY = playerline.Max(p => p.Y);

      float playerCenterX = (playerMinX + playerMaxX) / 2f;
      float playerCenterY = (playerMinY + playerMaxY) / 2f;

      Vector2 offset = new Vector2(-1f, 0f);
      for (int i = 0; i < playerline.Count; i++)
      {
        playerline[i] -= new Vector2(playerCenterX, playerCenterY);
        playerline[i] += offset;

      }

      // string fileNameNoExt = Path.GetFileNameWithoutExtension(binOutputPath);
      // string outputDir = $"Output/CSV/{fileNameNoExt}";
      // if (!Directory.Exists(outputDir))
      //     Directory.CreateDirectory(outputDir);

      // string fullBinPath = $"{outputDir}/{fileNameNoExt}.bin";

      // using (var writer = new BinaryWriter(File.Create(fullBinPath)))
      // {
      //     WritePoints(writer, edgeData.OuterBoundary);
      //     WritePoints(writer, edgeData.InnerBoundary);
      //     WritePoints(writer, edgeData.Raceline);
      //     WritePoints(writer, playerline);
      // }

      //Console.WriteLine($"Combined edge + playerline data written to: {fullBinPath}");
      PlayerLine result = new PlayerLine
      {
        InnerBoundary = edgeData.InnerBoundary,
        OuterBoundary = edgeData.OuterBoundary,
        Raceline = edgeData.Raceline,
        PlayerPath = playerline
      };

      return result;
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
