//Used for getting the edge data from a binary file
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

public class EdgeData
{
    public List<Vector2> OuterBoundary { get; private set; } = new List<Vector2>();
    public List<Vector2> InnerBoundary { get; private set; } = new List<Vector2>();

    public static EdgeData LoadFromBinary(string filePath)
    {
        var edgeData = new EdgeData();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(fs))
        {
            edgeData.OuterBoundary = ReadPoints(br);
            edgeData.InnerBoundary = ReadPoints(br);
        }

        return edgeData;
    }

        public static EdgeData LoadFromLists(List<Vector2> outerBoundary, List<Vector2> innerBoundary)
    {
        var edgeData = new EdgeData();

        edgeData.OuterBoundary = new List<Vector2>(outerBoundary);
        edgeData.InnerBoundary = new List<Vector2>(innerBoundary);

        return edgeData;
    }

    public float GetAverageTrackWidth()
    {
        int count = Math.Min(InnerBoundary.Count, OuterBoundary.Count);
        if (count == 0) return 0f;

        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            total += Vector2.Distance(InnerBoundary[i], OuterBoundary[i]);
        }

        return total / count;
    }    

    public Vector2 GetCenter()
    {
        int count = Math.Min(InnerBoundary.Count, OuterBoundary.Count);
        Vector2 sum = Vector2.Zero;

        for (int i = 0; i < count; i++)
        {
            sum += (InnerBoundary[i] + OuterBoundary[i]) * 0.5f;
        }

        return sum / count;
    }

    public void ScaleTrack(Vector2 origin, float scaleFactor)
    {
        for (int i = 0; i < OuterBoundary.Count; i++)
        {
            OuterBoundary[i] = origin + (OuterBoundary[i] - origin) * scaleFactor;
        }

        for (int i = 0; i < InnerBoundary.Count; i++)
        {
            InnerBoundary[i] = origin + (InnerBoundary[i] - origin) * scaleFactor;
        }
    }


    private static List<Vector2> ReadPoints(BinaryReader br)
    {
        List<Vector2> points = new List<Vector2>();
        int numPoints = br.ReadInt32();

        for (int i = 0; i < numPoints; i++)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            points.Add(new Vector2(x, y));
        }

        return points;
    }
}
