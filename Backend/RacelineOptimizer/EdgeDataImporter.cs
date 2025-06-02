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
