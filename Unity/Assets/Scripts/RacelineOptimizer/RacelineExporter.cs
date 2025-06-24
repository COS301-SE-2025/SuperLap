using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace RacelineOptimizer
{
    public static class RacelineExporter
    {
        public static void SaveToBinary(string filePath, List<Vector2> inner, List<Vector2> outer, List<Vector2> raceline)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                WritePoints(bw, outer);     // First: OuterBoundary
                WritePoints(bw, inner);     // Second: InnerBoundary
                WritePoints(bw, raceline);  // Third: Raceline

                bw.Write(0);
            }
        }

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
