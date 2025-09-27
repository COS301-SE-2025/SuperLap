using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions;
using LibTessDotNet;

[System.Serializable]
public class MotoGPDisplayData
{
  public List<Vector2> OuterBoundary;
  public List<Vector2> InnerBoundary;
  public List<Vector2> Raceline;
  public List<Vector2> PlayerPath;
  public List<Vector2> WorstDeviationSections;
}

public class ShowMotoGP : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
{
  [Header("Display Settings")]
  [SerializeField] private RectTransform trackContainer;
  [SerializeField] private RectTransform zoomContainer;
  [SerializeField] private RectTransform viewportRect;

  [Header("Line Renderer Settings")]
  [SerializeField] private Material lineMaterial;
  [SerializeField] private float outerBoundaryWidth = 1f;
  [SerializeField] private float innerBoundaryWidth = 1f;
  [SerializeField] private float racelineWidth = 1f;
  [SerializeField] private float playerPathWidth = 1f;
  [SerializeField] private float deviationSectionWidth = 1f;

  [Header("Track Colors")]
  [SerializeField] private Color outerBoundaryColor = Color.blue;
  [SerializeField] private Color innerBoundaryColor = Color.red;
  [SerializeField] private Color roadColor = new Color(0.2f, 0.2f, 0.2f, 1);
  [SerializeField] private Color racelineColor = Color.green;
  [SerializeField] private Color playerRaceLineColor = Color.blue;
  [SerializeField] private Color deviationSectionColor = new Color(1, 0.4f, 0, 1);

  [Header("Track Controls")]
  [SerializeField] private bool showOuterBoundary = true;
  [SerializeField] private bool showInnerBoundary = true;
  [SerializeField] private bool showRaceLine = true;
  [SerializeField] private bool showPlayerRaceLine = true;
  [SerializeField] private bool showDeviationSections = true;

  [Header("Zoom/Pan Settings")]
  [SerializeField] private float zoomSpeed = 0.1f;
  [SerializeField] private float minZoom = 0.5f;
  [SerializeField] private float maxZoom = 3f;
  [SerializeField] private float panSpeed = 1f;
  [SerializeField] private bool invertZoom = false;

  private MotoGPDisplayData currentTrackData;
  private Dictionary<string, UILineRenderer> lineRenderers = new Dictionary<string, UILineRenderer>();
  private List<UILineRenderer> deviationLineRenderers = new List<UILineRenderer>();
  private float currentZoom = 1f;
  private Vector2 panOffset = Vector2.zero;
  private Vector2 initialPosition;
  private bool isDragging = false;
  private Vector2 dragStartPosition;

  void Start()
  {
    if (!zoomContainer && trackContainer) zoomContainer = trackContainer.parent as RectTransform;
    if (!viewportRect) viewportRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    initialPosition = zoomContainer.anchoredPosition;
    deviationSectionWidth = playerPathWidth;
    UpdateLineWidths();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (currentZoom > 1f)
    {
      isDragging = true;
      dragStartPosition = eventData.position;
    }
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (currentZoom <= 1f || !isDragging) return;

    panOffset += (eventData.position - dragStartPosition) * panSpeed;
    dragStartPosition = eventData.position;
    ConstrainToViewport();
    UpdateZoomContainer();
  }

  public void OnScroll(PointerEventData eventData)
  {
    float zoomDelta = eventData.scrollDelta.y * zoomSpeed * (invertZoom ? -1 : 1);
    float previousZoom = currentZoom;
    currentZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);

    if (!Mathf.Approximately(previousZoom, currentZoom))
    {
      RectTransformUtility.ScreenPointToLocalPointInRectangle(zoomContainer, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
      float zoomRatio = currentZoom / previousZoom;
      panOffset = (panOffset - localPoint) * zoomRatio + localPoint;
    }

    if (Mathf.Approximately(currentZoom, 1f)) panOffset = Vector2.zero;
    ConstrainToViewport();
    UpdateZoomContainer();
    UpdateLineWidths();
  }

  public void ResetView()
  {
    currentZoom = 1f;
    panOffset = Vector2.zero;
    UpdateZoomContainer();
    UpdateLineWidths();
  }

  public void changeOuterBoundaryWidth(float newWidth)
  {
    outerBoundaryWidth = newWidth;

    if (lineRenderers.TryGetValue("OuterBoundary", out UILineRenderer renderer))
    {
      renderer.LineThickness = outerBoundaryWidth / currentZoom;
    }
  }

  public void changeInnerBoundaryWidth(float newWidth)
  {
    innerBoundaryWidth = newWidth;

    if (lineRenderers.TryGetValue("InnerBoundary", out UILineRenderer renderer))
    {
      renderer.LineThickness = innerBoundaryWidth / currentZoom;
    }
  }

  public void changeRaceLineWidth(float newWidth)
  {
    racelineWidth = newWidth;

    if (lineRenderers.TryGetValue("Raceline", out UILineRenderer renderer))
    {
      renderer.LineThickness = racelineWidth / currentZoom;
    }
  }

  public void changePlayerPathWidth(float newWidth)
  {
    playerPathWidth = newWidth;
    deviationSectionWidth = newWidth;

    foreach (var kvp in lineRenderers)
    {
      if (kvp.Key.StartsWith("PlayerPath_Segment_") && kvp.Value != null)
      {
        kvp.Value.LineThickness = playerPathWidth / currentZoom;
      }
    }
    foreach (var renderer in deviationLineRenderers)
    {
      if (renderer != null)
      {
        renderer.LineThickness = deviationSectionWidth / currentZoom;
      }
    }
  }

  public void changeDeviationSectionWidth(float newWidth)
  {
    deviationSectionWidth = newWidth;
    
    foreach (var renderer in deviationLineRenderers)
    {
      if (renderer != null)
      {
        renderer.LineThickness = deviationSectionWidth / currentZoom;
      }
    }
  }

  private void ConstrainToViewport()
  {
    if (!viewportRect || !trackContainer) return;

    Vector2 scaledSize = trackContainer.rect.size * currentZoom;
    Vector2 viewportSize = viewportRect.rect.size;
    Vector2 maxOffset = Vector2.Max((scaledSize - viewportSize) * 0.5f, Vector2.zero);

    panOffset.x = Mathf.Clamp(panOffset.x, -maxOffset.x, maxOffset.x);
    panOffset.y = Mathf.Clamp(panOffset.y, -maxOffset.y, maxOffset.y);
  }

  private void UpdateZoomContainer()
  {
    if (!zoomContainer) return;
    zoomContainer.localScale = Vector3.one * currentZoom;
    zoomContainer.anchoredPosition = initialPosition + panOffset;
  }

  private void UpdateLineWidths()
  {
    deviationSectionWidth = playerPathWidth;
    foreach (var kvp in lineRenderers)
    {
      if (!kvp.Value) continue;

      float baseWidth = 1f;
      if (kvp.Key == "OuterBoundary") baseWidth = outerBoundaryWidth;
      else if (kvp.Key == "InnerBoundary") baseWidth = innerBoundaryWidth;
      else if (kvp.Key == "Raceline") baseWidth = racelineWidth;
      else if (kvp.Key.StartsWith("PlayerPath_Segment_")) baseWidth = playerPathWidth;

      kvp.Value.LineThickness = baseWidth / currentZoom;
    }

    foreach (var renderer in deviationLineRenderers)
    {
      if (renderer != null)
      {
        renderer.LineThickness = deviationSectionWidth / currentZoom;
      }
    }
  }

  private List<Vector2> EnsureLooped(List<Vector2> points)
  {
    if (points == null || points.Count < 2)
      return points;

    if (points[0] != points[points.Count - 1])
    {
      points.Add(points[0]);
    }
    return points;
  }
  
  private List<Vector2> Downsample(List<Vector2> points, int step)
  {
    return points.Where((pt, idx) => idx % step == 0).ToList();
  }
  
  private List<Vector2> EnsureBelowLimit(List<Vector2> points, int limit = 64000)
  {
    if (points == null)
      return points;

    int step = 2; 
    while (points.Count > limit)
    {
      points = Downsample(points, step);
    }

    return points;
  }

  public void DisplayPlayerLineData(CSVToBinConverter.LoadCSV.PlayerLine playerLine)
  {
    float simplificationTolerance = 3f;

    MotoGPDisplayData displayData = new MotoGPDisplayData
    {
      PlayerPath = EnsureBelowLimit(ConvertToUnityVector2(playerLine.PlayerPath)),
      InnerBoundary = LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(EnsureLooped(EnsureBelowLimit(ConvertToUnityVector2(playerLine.InnerBoundary))), simplificationTolerance)), simplificationTolerance)),
      OuterBoundary = LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(EnsureLooped(EnsureBelowLimit(ConvertToUnityVector2(playerLine.OuterBoundary))), simplificationTolerance)), simplificationTolerance)),
      Raceline = EnsureLooped(EnsureBelowLimit(ConvertToUnityVector2(playerLine.Raceline))),
      WorstDeviationSections = ConvertToUnityVector2(playerLine.WorstDeviationSections) // Added this conversion
    };

    DisplayRacelineData(displayData);
  }

  List<UnityEngine.Vector2> ConvertToUnityVector2(List<System.Numerics.Vector2> list)
  {
    return list.Select(v => new UnityEngine.Vector2(v.X, v.Y)).ToList();
  }

  public void DisplayRacelineData(MotoGPDisplayData trackData)
  {
    if (!trackContainer || trackData == null) return;

    ClearExistingLines();
    currentTrackData = trackData;
    (Vector2 min, Vector2 max, Vector2 size) bounds = CalculateBounds(trackData);
    float scale = CalculateScale(bounds.size);
    Vector2 offset = CalculateOffset(bounds.size, scale);

    CreateRoadArea(trackData.OuterBoundary, trackData.InnerBoundary, bounds.min, scale, offset);

    if (showOuterBoundary) CreateLineRenderer("OuterBoundary", trackData.OuterBoundary, outerBoundaryColor, outerBoundaryWidth, bounds.min, scale, offset);
    if (showInnerBoundary) CreateLineRenderer("InnerBoundary", trackData.InnerBoundary, innerBoundaryColor, innerBoundaryWidth, bounds.min, scale, offset);
    if (showRaceLine) CreateLineRenderer("Raceline", trackData.Raceline, racelineColor, racelineWidth, bounds.min, scale, offset);

    if (showPlayerRaceLine && trackData.PlayerPath != null)
    {
      CreatePlayerPathWithDeviations(trackData.PlayerPath, trackData.WorstDeviationSections, bounds.min, scale, offset);
    }

    ResetView();
  }
  private void CreatePlayerPathWithDeviations(List<Vector2> playerPath, List<Vector2> deviationSections, Vector2 min, float scale, Vector2 offset)
  {
    if (playerPath == null || playerPath.Count < 2) return;

    bool[] isDeviationIndex = new bool[playerPath.Count];
    
    if (deviationSections != null && showDeviationSections)
    {
      foreach (Vector2 section in deviationSections)
      {
        int startIndex = Mathf.FloorToInt(section.x);
        int endIndex = Mathf.FloorToInt(section.y);
        
        for (int i = Mathf.Max(0, startIndex); i <= Mathf.Min(playerPath.Count - 1, endIndex); i++)
        {
          isDeviationIndex[i] = true;
        }
      }
    }

    List<Vector2> currentSegment = new List<Vector2>();
    bool currentIsDeviation = false;
    int segmentCount = 0;

    for (int i = 0; i < playerPath.Count; i++)
    {
      bool thisIsDeviation = showDeviationSections && isDeviationIndex[i];
      if (i == 0 || thisIsDeviation != currentIsDeviation)
      {
        if (currentSegment.Count >= 2)
        {
          CreatePlayerPathSegment(currentSegment, currentIsDeviation, segmentCount, min, scale, offset);
          segmentCount++;
        }
        
        currentSegment.Clear();
        currentIsDeviation = thisIsDeviation;
        
        if (i > 0)
        {
          currentSegment.Add(playerPath[i - 1]);
        }
      }
      
      currentSegment.Add(playerPath[i]);
    }

    if (currentSegment.Count >= 2)
    {
      CreatePlayerPathSegment(currentSegment, currentIsDeviation, segmentCount, min, scale, offset);
    }

    Debug.Log($"Created {segmentCount + 1} player path segments");
  }

  private void CreatePlayerPathSegment(List<Vector2> segmentPoints, bool isDeviation, int segmentIndex, Vector2 min, float scale, Vector2 offset)
  {
    Color segmentColor = isDeviation ? deviationSectionColor : playerRaceLineColor;
    float segmentWidth = isDeviation ? deviationSectionWidth : playerPathWidth;
    string segmentName = isDeviation ? $"PlayerPath_Deviation_{segmentIndex}" : $"PlayerPath_Normal_{segmentIndex}";

    UILineRenderer lr = new GameObject(segmentName, typeof(RectTransform), typeof(UILineRenderer)).GetComponent<UILineRenderer>();
    lr.transform.SetParent(trackContainer, false);
    lr.material = lineMaterial;
    lr.color = segmentColor;
    lr.LineThickness = segmentWidth / currentZoom;
    lr.Points = segmentPoints.ConvertAll(p => TransformPoint(p, min, scale, offset)).ToArray();
    
    if (isDeviation)
    {
      deviationLineRenderers.Add(lr);
    }
    else
    {
      lineRenderers[$"PlayerPath_Segment_{segmentIndex}"] = lr;
    }
  }

  private void ClearExistingLines()
  {
    foreach (Transform child in trackContainer) Destroy(child.gameObject);
    lineRenderers.Clear();
    deviationLineRenderers.Clear();
  }

  private (Vector2 min, Vector2 max, Vector2 size) CalculateBounds(MotoGPDisplayData trackData)
  {
    Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
    Vector2 max = new Vector2(float.MinValue, float.MinValue);

    void UpdateBounds(List<Vector2> points)
    {
      if (points == null) return;
      for (int i = 0; i < points.Count; i++)
      {
        min = Vector2.Min(min, points[i]);
        max = Vector2.Max(max, points[i]);
      }
    }

    UpdateBounds(trackData.OuterBoundary);
    UpdateBounds(trackData.InnerBoundary);
    UpdateBounds(trackData.Raceline);
    UpdateBounds(trackData.PlayerPath);

    return (min, max, max - min);
  }

  private float CalculateScale(Vector2 size)
  {
    float margin = 50f;
    return Mathf.Min((trackContainer.rect.width - 2 * margin) / size.x, (trackContainer.rect.height - 2 * margin) / size.y);
  }

  private Vector2 CalculateOffset(Vector2 size, float scale)
  {
    Vector2 scaledSize = size * scale;
    return new Vector2(
        (trackContainer.rect.width - scaledSize.x) * 0.5f,
        (trackContainer.rect.height - scaledSize.y) * 0.5f
    );
  }

  private Vector2 TransformPoint(Vector2 point, Vector2 min, float scale, Vector2 offset)
  {
    Vector2 transformed = (point - min) * scale + offset;
    transformed.y = trackContainer.rect.height - transformed.y;
    return transformed - trackContainer.rect.size * 0.5f;
  }

  private void CreateRoadArea(List<Vector2> outer, List<Vector2> inner, Vector2 min, float scale, Vector2 offset)
  {
    if (outer == null || inner == null || outer.Count < 3 || inner.Count < 3) return;

    List<Vector2> combined = new List<Vector2>(outer.Count + inner.Count);
    combined.AddRange(outer);
    inner.Reverse();
    combined.AddRange(inner);

    GameObject roadGO = new GameObject("RoadMesh", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIMeshPolygon));
    roadGO.transform.SetParent(trackContainer, false);

    UIMeshPolygon roadMesh = roadGO.GetComponent<UIMeshPolygon>();
    roadMesh.Points = combined.ConvertAll(p => TransformPoint(p, min, scale, offset));
    roadMesh.color = roadColor;
  }

  private void CreateLineRenderer(string key, List<Vector2> points, Color color, float width, Vector2 min, float scale, Vector2 offset)
  {
    if (points == null || points.Count < 2) return;

    UILineRenderer lr = new GameObject(key, typeof(RectTransform), typeof(UILineRenderer)).GetComponent<UILineRenderer>();
    lr.transform.SetParent(trackContainer, false);
    lr.material = lineMaterial;
    lr.color = color;
    lr.LineThickness = width / currentZoom;
    lr.Points = points.ConvertAll(p => TransformPoint(p, min, scale, offset)).ToArray();
    lineRenderers[key] = lr;
  }
}