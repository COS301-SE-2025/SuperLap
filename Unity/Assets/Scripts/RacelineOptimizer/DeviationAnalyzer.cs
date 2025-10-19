using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
public static class DeviationAnalyzer
{
  private class DeviationSection
  {
    public int StartIndex;
    public int EndIndex;
    public float AvgDeviation;
  }

  private static float DistanceToNearest(Vector2 point, List<Vector2> raceline)
  {
    float minDist = float.MaxValue;
    foreach (var r in raceline)
    {
      float dx = point.X - r.X;
      float dy = point.Y - r.Y;
      float dist = dx * dx + dy * dy;
      if (dist < minDist)
        minDist = dist;
    }
    return MathF.Sqrt(minDist);
  }

  private static List<DeviationSection> FindDeviationSections(List<float> deviations, float threshold)
  {
    var sections = new List<DeviationSection>();
    int start = -1;
    float sum = 0;
    int count = 0;

    for (int i = 0; i < deviations.Count; i++)
    {
      if (deviations[i] > threshold)
      {
        if (start == -1) start = i;
        sum += deviations[i];
        count++;
      }
      else if (start != -1)
      {
        sections.Add(new DeviationSection
        {
          StartIndex = start,
          EndIndex = i - 1,
          AvgDeviation = sum / count
        });
        start = -1;
        sum = 0;
        count = 0;
      }
    }

    if (start != -1 && count > 0)
    {
      sections.Add(new DeviationSection
      {
        StartIndex = start,
        EndIndex = deviations.Count - 1,
        AvgDeviation = sum / count
      });
    }

    return sections;
  }

  public static List<Vector2> GetWorstDeviationSections(List<Vector2> playerPath, List<Vector2> raceline, int topN = 5)
  {
    if (playerPath == null || raceline == null || playerPath.Count == 0 || raceline.Count == 0)
      return new List<Vector2>();

    // Build deviation profile
    List<float> deviations = new();
    foreach (var p in playerPath)
      deviations.Add(DistanceToNearest(p, raceline));

    var mean = deviations.Average();
    var sections = FindDeviationSections(deviations, mean);

    return sections
        .OrderByDescending(s => s.AvgDeviation)
        .Take(topN)
        .Select(s => new Vector2(s.StartIndex, s.EndIndex))
        .ToList();
  }
}
