using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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

    DrawLineWithMesh();
  }

  public List<ReplayState> getReplays()
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

  private void DrawLineWithMesh()
  {
    if (meshFilter == null)
    {
      meshFilter = gameObject.AddComponent<MeshFilter>();
      meshRenderer = gameObject.AddComponent<MeshRenderer>();
      Material mat = new Material(Shader.Find("Sprites/Default"));
      meshRenderer.material = mat;
    }

    if (replayStates.Count < 2) return;

    // Apply color weighting to create weighted colors array
    Color[] weightedColors = ApplyColorWeighting();

    List<Vector3> vertices = new List<Vector3>();
    List<Color> colors = new List<Color>();
    List<int> triangles = new List<int>();

    float lineWidth = 10.0f;

    for (int i = 0; i < replayStates.Count - 1; i++)
    {
      Vector3 pos1 = new Vector3(replayStates[i].position.X, 1, replayStates[i].position.Y);
      Vector3 pos2 = new Vector3(replayStates[i + 1].position.X, 1, replayStates[i + 1].position.Y);

      Vector3 direction = (pos2 - pos1).normalized;
      Vector3 perpendicular = Vector3.Cross(direction, Vector3.up) * lineWidth * 0.5f;

      // Use the weighted color instead of the original throttle color
      Color segmentColor = weightedColors[i];

      int vertexIndex = vertices.Count;

      // Create quad for this segment
      vertices.Add(pos1 - perpendicular);
      vertices.Add(pos1 + perpendicular);
      vertices.Add(pos2 + perpendicular);
      vertices.Add(pos2 - perpendicular);

      colors.Add(segmentColor);
      colors.Add(segmentColor);
      colors.Add(segmentColor);
      colors.Add(segmentColor);

      // Create triangles for the quad
      triangles.Add(vertexIndex);
      triangles.Add(vertexIndex + 1);
      triangles.Add(vertexIndex + 2);

      triangles.Add(vertexIndex);
      triangles.Add(vertexIndex + 2);
      triangles.Add(vertexIndex + 3);
    }

    Mesh mesh = new Mesh();
    mesh.vertices = vertices.ToArray();
    mesh.colors = colors.ToArray();
    mesh.triangles = triangles.ToArray();
    mesh.RecalculateNormals();

    meshFilter.mesh = mesh;
  }

  private Color[] ApplyColorWeighting()
  {
    Color[] weightedColors = new Color[replayStates.Count];
    int segmentSize = 50;

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
      if (redCount >= 10)
      {
        segmentColor = Color.red;
      }
      else if (yellowCount >= 25)
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

  public void Update()
  {
    if (Input.GetKeyDown(KeyCode.Space))
    {
      playing = !playing;
    }

    if (playing)
    {
      if (remainingTime > 0)
      {
        remainingTime -= Time.deltaTime;
      }
      else
      {
        UpdateView();
      }
    }
  }

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
}