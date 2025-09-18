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
}

public class ShowMotoGP : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
{
  [Header("Display Settings")]
  public RectTransform trackContainer;
  public RectTransform zoomContainer;
  public RectTransform viewportRect;

  [Header("Line Renderer Settings")]
  public Material lineMaterial;
  public float outerBoundaryWidth = 1f;
  public float innerBoundaryWidth = 1f;
  public float racelineWidth = 1f;
  public float playerPathWidth = 1f;

  [Header("Track Colors")]
  public Color outerBoundaryColor = Color.blue;
  public Color innerBoundaryColor = Color.red;
  public Color roadColor = new Color(0.2f, 0.2f, 0.2f, 1);
  public Color racelineColor = Color.green;
  public Color playerRaceLineColor = new Color(0, 0.5f, 1, 1);

  [Header("Track Controls")]
  public bool showOuterBoundary = true;
  public bool showInnerBoundary = true;
  public bool showRaceLine = true;
  public bool showPlayerRaceLine = true;

  [Header("Zoom/Pan Settings")]
  public float zoomSpeed = 0.1f;
  public float minZoom = 0.5f;
  public float maxZoom = 3f;
  public float panSpeed = 1f;
  public bool invertZoom = false;

  private MotoGPDisplayData currentTrackData;
  private Dictionary<string, UILineRenderer> lineRenderers = new Dictionary<string, UILineRenderer>();
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

    if (lineRenderers.TryGetValue("PlayerPath", out UILineRenderer renderer))
    {
      renderer.LineThickness = playerPathWidth / currentZoom;
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
    foreach (var kvp in lineRenderers)
    {
      if (!kvp.Value) continue;
      float baseWidth = kvp.Key switch
      {
        "OuterBoundary" => outerBoundaryWidth,
        "InnerBoundary" => innerBoundaryWidth,
        "Raceline" => racelineWidth,
        "PlayerPath" => playerPathWidth,
        _ => 1f
      };
      kvp.Value.LineThickness = baseWidth / currentZoom;
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
      Raceline = EnsureLooped(EnsureBelowLimit(ConvertToUnityVector2(playerLine.Raceline)))
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
    if (showPlayerRaceLine) CreateLineRenderer("PlayerPath", trackData.PlayerPath, playerRaceLineColor, playerPathWidth, bounds.min, scale, offset);

    ResetView();
  }


  private void ClearExistingLines()
  {
    foreach (Transform child in trackContainer) Destroy(child.gameObject);
    lineRenderers.Clear();
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
