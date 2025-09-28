using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class ACOAgentReplay : MonoBehaviour
{
  List<ReplayState> replayStates;
  int updateRate = 50;
  float remainingTime = 0;
  bool playing = false;
  public GameObject model;
  private int currentStep = 0;
  private MeshRenderer meshRenderer;
  private MeshFilter meshFilter;

  public void InitializeString(string data)
  {
    string temp = data;
    replayStates = new();

    while (temp.Length > 0)
    {
      int pos = temp.IndexOf('\n');
      string line = temp[..pos];
      temp = temp[(pos + 1)..];
      if (line.StartsWith("#")) continue;
      replayStates.Add(ReplayState.Parse(line));
    }

    //DrawLineWithMesh();
  }

  public void InitializeReplay(List<ReplayState> states)
  {
    replayStates = states;
  }

  private static void WritePoints(BinaryWriter writer, List<Vector2> points)
  {
    writer.Write(points.Count);
    foreach (var pt in points)
    {
      writer.Write(pt.x);
      writer.Write(pt.y);
    }
  }

  public List<ReplayState> GetReplays()
  {
    return replayStates;
  }

  public List<(Vector2 start, Vector2 end, Color color)> GetColoredSegments()
  {
    var segments = new List<(Vector2, Vector2, Color)>();
    if (replayStates == null || replayStates.Count < 2) return segments;

    Color[] weightedColors = ApplyColorWeighting();

    for (int i = 0; i < replayStates.Count - 1; i++)
    {
      Vector2 pos1 = new Vector2(replayStates[i].position.X, replayStates[i].position.Y);
      Vector2 pos2 = new Vector2(replayStates[i + 1].position.X, replayStates[i + 1].position.Y);

      segments.Add((pos1, pos2, weightedColors[i]));
    }

    return segments;
  }

  private Color[] ApplyColorWeighting()
  {
    Color[] weightedColors = new Color[replayStates.Count];
    int segmentSize = 100;

    for (int i = 0; i < replayStates.Count; i++)
    {
      // Determine the window for this segment
      int windowStart = (i / segmentSize) * segmentSize;
      int windowEnd = Mathf.Min(windowStart + segmentSize, replayStates.Count);

      // Count colors in this window
      int redCount = 0;
      int yellowCount = 0;
      int greenCount = 0;

      for (int j = windowStart; j < windowEnd; j++)
      {
        float throttle = replayStates[j].throttle;
        if (throttle == 0) yellowCount++;
        else if (throttle == 1) greenCount++;
        else redCount++;
      }

      // Apply color rules
      Color segmentColor;
      if (redCount >= 15)
      {
        segmentColor = Color.red;
      }
      else if (yellowCount >= 15)
      {
        segmentColor = Color.yellow;
      }
      else
      {
        segmentColor = Color.green;
      }

      weightedColors[i] = segmentColor;
    }

    return weightedColors;
  }

  public void InitializeTextFile(string fileName)
  {
    string data = "";
    using (StreamReader reader = new StreamReader(fileName))
    {
      data = reader.ReadToEnd();
    }

    InitializeString(data);
  }

  // public void Update()
  // {
  //   if (Input.GetKeyDown(KeyCode.Space))
  //   {
  //     playing = !playing;
  //   }

  //   if (playing)
  //   {
  //     if (remainingTime > 0)
  //     {
  //       remainingTime -= Time.deltaTime;
  //     }
  //     else
  //     {
  //       UpdateView();
  //     }
  //   }
  // }

  public void Start()
  {

  }

  private void UpdateView()
  {
    remainingTime += 1.0f / updateRate;
    ReplayState state = replayStates[currentStep];
    model.transform.position = new Vector3(state.position.X, 1, state.position.Y);
    model.transform.rotation = Quaternion.Euler(0.0f, state.bear, 0.0f);
    currentStep++;

    if (currentStep >= replayStates.Count)
    {
      currentStep = 0; // Loop back to start
    }
  }

  public List<(Vector2 start, Vector2 end, Color color)> SimplifyColoredSegments(List<(Vector2 start, Vector2 end, Color color)> segments, float tolerance = 5.0f)
  {
    if (segments == null || segments.Count == 0) 
      return new List<(Vector2, Vector2, Color)>();

    var simplifiedSegments = new List<(Vector2, Vector2, Color)>();
    
    // Group consecutive segments by color
    var colorGroups = new List<List<(Vector2 start, Vector2 end, Color color)>>();
    var currentGroup = new List<(Vector2 start, Vector2 end, Color color)>();
    Color currentColor = segments[0].color;
    
    foreach (var segment in segments)
    {
      if (segment.color.Equals(currentColor))
      {
        currentGroup.Add(segment);
      }
      else
      {
        colorGroups.Add(new List<(Vector2, Vector2, Color)>(currentGroup));
        currentGroup.Clear();
        currentGroup.Add(segment);
        currentColor = segment.color;
      }
    }
    if (currentGroup.Count > 0)
      colorGroups.Add(currentGroup);

    // Simplify each color group independently
    foreach (var group in colorGroups)
    {
      var simplifiedGroup = SimplifyColorGroup(group, tolerance);
      simplifiedSegments.AddRange(simplifiedGroup);
    }
    
    return simplifiedSegments;
  }

  private List<(Vector2 start, Vector2 end, Color color)> SimplifyColorGroup(List<(Vector2 start, Vector2 end, Color color)> group, float tolerance)
  {
    if (group.Count <= 1) return group;

    // Extract points from the segments to form a continuous path
    List<Vector2> points = new List<Vector2>();
    points.Add(group[0].start);
    foreach (var segment in group)
    {
      points.Add(segment.end);
    }

    // Simplify the path using Douglas-Peucker
    List<Vector2> simplifiedPoints = DouglasPeucker(points, tolerance);

    // Convert back to segments with the original color
    var simplifiedSegments = new List<(Vector2, Vector2, Color)>();
    Color groupColor = group[0].color;

    for (int i = 0; i < simplifiedPoints.Count - 1; i++)
    {
      simplifiedSegments.Add((simplifiedPoints[i], simplifiedPoints[i + 1], groupColor));
    }

    return simplifiedSegments;
  }
  
  private List<Vector2> DouglasPeucker(List<Vector2> points, float tolerance)
  {
    if (points.Count <= 2) return new List<Vector2>(points);
    
    // Find the point with maximum distance from line segment
    float maxDistance = 0;
    int maxIndex = 0;
    Vector2 start = points[0];
    Vector2 end = points[points.Count - 1];
    
    for (int i = 1; i < points.Count - 1; i++)
    {
      float distance = PointToLineDistance(points[i], start, end);
      if (distance > maxDistance)
      {
        maxDistance = distance;
        maxIndex = i;
      }
    }
    
    List<Vector2> result = new List<Vector2>();
    
    // If max distance is greater than tolerance, recursively simplify
    if (maxDistance > tolerance)
    {
      // Recursively simplify both parts
      List<Vector2> leftPart = DouglasPeucker(points.GetRange(0, maxIndex + 1), tolerance);
      List<Vector2> rightPart = DouglasPeucker(points.GetRange(maxIndex, points.Count - maxIndex), tolerance);
      
      // Combine results (remove duplicate middle point)
      result.AddRange(leftPart);
      result.AddRange(rightPart.Skip(1));
    }
    else
    {
      // Keep only start and end points
      result.Add(start);
      result.Add(end);
    }
    
    return result;
  }

  private float PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
  {
    Vector2 line = lineEnd - lineStart;
    float lineLength = line.magnitude;
    
    if (lineLength == 0) return Vector2.Distance(point, lineStart);
    
    float t = Mathf.Clamp01(Vector2.Dot(point - lineStart, line) / (lineLength * lineLength));
    Vector2 projection = lineStart + t * line;
    
    return Vector2.Distance(point, projection);
  }

  public void SaveBinFile(float simplificationTolerance = 5.0f)
  {
    string outputPath = Path.Combine(Application.streamingAssetsPath, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_simplified_breakPoints.bin");
    
    // Get colored segments first
    List<(Vector2 start, Vector2 end, Color color)> segments = GetColoredSegments();

    // Simplify the colored segments while preserving color boundaries
    List<(Vector2 start, Vector2 end, Color color)> simplifiedSegments = SimplifyColoredSegments(segments, simplificationTolerance);

    // Extract the simplified player line from the simplified segments
    List<Vector2> simplifiedPlayerLine = new List<Vector2>();
    if (simplifiedSegments.Count > 0)
    {
      simplifiedPlayerLine.Add(simplifiedSegments[0].start);
      foreach (var segment in simplifiedSegments)
      {
        simplifiedPlayerLine.Add(segment.end);
      }
    }

    // Group simplified segments by color for output
    List<Vector2> redSegments = new List<Vector2>();
    List<Vector2> yellowSegments = new List<Vector2>();
    List<Vector2> greenSegments = new List<Vector2>();

    foreach (var segment in simplifiedSegments)
    {
      if (segment.color.Equals(Color.red))
      {
        redSegments.Add(segment.start);
        redSegments.Add(segment.end);
      }
      else if (segment.color.Equals(Color.yellow))
      {
        yellowSegments.Add(segment.start);
        yellowSegments.Add(segment.end);
      }
      else if (segment.color.Equals(Color.green))
      {
        greenSegments.Add(segment.start);
        greenSegments.Add(segment.end);
      }
    }

    var trackData = ACOTrackMaster.instance.LastProcessingResults;
    string outputDir = Path.GetDirectoryName(outputPath);

    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
      Directory.CreateDirectory(outputDir);

    using (var writer = new BinaryWriter(File.Create(outputPath)))
    {
      WritePoints(writer, trackData.outerBoundary);
      WritePoints(writer, trackData.innerBoundary);
      WritePoints(writer, simplifiedPlayerLine); // Use simplified line
      WritePoints(writer, yellowSegments);
      WritePoints(writer, redSegments);
    }

    Debug.Log($"Simplified track with {simplifiedPlayerLine.Count} points (from {replayStates.Count}) written to: {outputPath}");
  }
}