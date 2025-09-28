using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions;
using LibTessDotNet;

public class UIMeshPolygon : MaskableGraphic
{
  [Header("Polygon Points")]
  public List<Vector2> Points = new List<Vector2>();
  private List<int> cachedIndices;
  private List<Vector2> lastPoints;
  private ContourVertex[] contour;

  protected override void OnPopulateMesh(VertexHelper vh)
  {
    vh.Clear();
    if (Points == null || Points.Count < 3) return;

    if (cachedIndices == null || lastPoints == null || !ReferenceEquals(lastPoints, Points))
    {
      cachedIndices = Triangulate(Points);
      lastPoints = Points;
    }

    for (int i = 0; i < Points.Count; i++)
      vh.AddVert(Points[i], color, Vector2.zero);

    for (int i = 0; i < cachedIndices.Count; i += 3)
      vh.AddTriangle(cachedIndices[i], cachedIndices[i + 1], cachedIndices[i + 2]);
  }

  private List<int> Triangulate(List<Vector2> points)
  {
    if (points == null || points.Count < 3)
      return new List<int>();

    var tess = new Tess();

    if (contour == null || contour.Length != points.Count)
      contour = new ContourVertex[points.Count];

    for (int i = 0; i < points.Count; i++)
    {
      contour[i].Position = new Vec3(points[i].x, points[i].y, 0);
      contour[i].Data = i;
    }

    tess.AddContour(contour, ContourOrientation.Original);
    tess.Tessellate(WindingRule.Positive, ElementType.Polygons, 3);

    var indices = new List<int>(tess.ElementCount * 3);
    for (int i = 0; i < tess.ElementCount; i++)
    {
      int i0 = tess.Elements[i * 3];
      int i1 = tess.Elements[i * 3 + 1];
      int i2 = tess.Elements[i * 3 + 2];

      if (i0 == -1 || i1 == -1 || i2 == -1) continue;

      var v0 = tess.Vertices[i0];
      var v1 = tess.Vertices[i1];
      var v2 = tess.Vertices[i2];

      if (v0.Data == null || v1.Data == null || v2.Data == null)
        continue;

      indices.Add((int)v0.Data);
      indices.Add((int)v1.Data);
      indices.Add((int)v2.Data);
    }


    return indices;
  }
}

public static class LineSimplifier
{
  public static List<Vector2> RamerDouglasPeucker(List<Vector2> points, float tolerance)
  {
    if (points == null || points.Count < 3)
      return new List<Vector2>(points);

    int firstIndex = 0;
    int lastIndex = points.Count - 1;
    List<int> pointIndicesToKeep = new List<int> { firstIndex, lastIndex };

    SimplifySection(points, firstIndex, lastIndex, tolerance, pointIndicesToKeep);

    pointIndicesToKeep.Sort();

    List<Vector2> simplifiedPoints = new List<Vector2>();
    foreach (int index in pointIndicesToKeep)
      simplifiedPoints.Add(points[index]);

    return simplifiedPoints;
  }

  private static void SimplifySection(List<Vector2> points, int firstIndex, int lastIndex, float tolerance, List<int> pointIndicesToKeep)
  {
    float maxDistance = 0f;
    int indexFarthest = 0;

    Vector2 firstPoint = points[firstIndex];
    Vector2 lastPoint = points[lastIndex];

    for (int i = firstIndex + 1; i < lastIndex; i++)
    {
      float distance = PerpendicularDistance(points[i], firstPoint, lastPoint);
      if (distance > maxDistance)
      {
        maxDistance = distance;
        indexFarthest = i;
      }
    }

    if (maxDistance > tolerance)
    {
      pointIndicesToKeep.Add(indexFarthest);

      SimplifySection(points, firstIndex, indexFarthest, tolerance, pointIndicesToKeep);
      SimplifySection(points, indexFarthest, lastIndex, tolerance, pointIndicesToKeep);
    }


  }

  private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
  {
    float dx = lineEnd.x - lineStart.x;
    float dy = lineEnd.y - lineStart.y;

    if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
      return Vector2.Distance(point, lineStart);

    float numerator = Mathf.Abs(dy * point.x - dx * point.y + lineEnd.x * lineStart.y - lineEnd.y * lineStart.x);
    float denominator = Mathf.Sqrt(dx * dx + dy * dy);
    return numerator / denominator;
  }

  public static List<Vector2> SmoothLine(List<Vector2> points, int iterations = 2)
  {
    for (int k = 0; k < iterations; k++)
    {
      List<Vector2> newPoints = new List<Vector2>();
      newPoints.Add(points[0]);

      for (int i = 0; i < points.Count - 1; i++)
      {
        Vector2 Q = Vector2.Lerp(points[i], points[i + 1], 0.25f);
        Vector2 R = Vector2.Lerp(points[i], points[i + 1], 0.75f);
        newPoints.Add(Q);
        newPoints.Add(R);
      }

      newPoints.Add(points[points.Count - 1]);
      points = newPoints;
    }

    return points;
  }
}
