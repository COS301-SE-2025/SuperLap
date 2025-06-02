using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace RacelineOptimizer
{
    public static class RacelineExporter
    {
        

        private static void WritePoints(BinaryWriter bw, List<Vector2> points)
        {
            bw.Write(points.Count);
            foreach (var pt in points)
            {
                bw.Write(pt.X);
                bw.Write(pt.Y);
            }
        }
    }
}
