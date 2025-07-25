using System.Numerics;

namespace RacelineOptimizer
{
    public static class RacelineExporter
    {
        public static void SaveToBinary(
            string filePath,
            List<Vector2> inner,
            List<Vector2> outer,
            List<Vector2> raceline,
            List<CornerDetector.CornerSegment>? corners = null)
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            WritePoints(bw, outer);     // First block: OuterBoundary
            WritePoints(bw, inner);     // Second block: InnerBoundary
            WritePoints(bw, raceline);  // Third block: Raceline

            if (corners != null && corners.Count > 0)
            {
                bw.Write(corners.Count); // Fourth block: Corner count
                Console.WriteLine($"Exporting {corners.Count} corners");
                foreach (var corner in corners)
                {
                    WriteVec2(bw, corner.InnerStart);
                    WriteVec2(bw, corner.InnerEnd);
                    WriteVec2(bw, corner.OuterStart);
                    WriteVec2(bw, corner.OuterEnd);
                    bw.Write(corner.Angle);
                    bw.Write(corner.IsLeftTurn);
                    bw.Write(corner.StartIndex);
                    bw.Write(corner.EndIndex);
                }
            }
            else
            {
                bw.Write(0); // No corners
            }
        }

        private static void WritePoints(BinaryWriter bw, List<Vector2> points)
        {
            bw.Write(points.Count);
            foreach (var pt in points)
                WriteVec2(bw, pt);
        }

        private static void WriteVec2(BinaryWriter bw, Vector2 pt)
        {
            bw.Write(pt.X);
            bw.Write(pt.Y);
        }
    }
}
