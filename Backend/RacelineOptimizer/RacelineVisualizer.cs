using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace RacelineVisualizer
{
    public class EdgeDataVisualizer
    {
        public class EdgeData
        {
            public List<Point> OuterBoundary { get; set; } = new();
            public List<Point> InnerBoundary { get; set; } = new();
            public List<Point> Raceline { get; set; } = new();
            public List<Point> Playerline { get; set; } = new(); // NEW
            public List<CornerSegment> Corners { get; set; } = new();
        }

        public struct CornerSegment
        {
            public Point InnerStart;
            public Point InnerEnd;
            public Point OuterStart;
            public Point OuterEnd;

            public float Angle;
            public bool IsLeftTurn;
            public int StartIndex;
            public int EndIndex;
        }

        public static EdgeData ReadEdgesFromBin(
            string path,
            bool includeRaceline = false,
            bool includeCorners = false,
            bool includePlayerline = false) // NEW
        {
            EdgeData data = new();

            using var reader = new BinaryReader(File.OpenRead(path));
            try
            {
                data.OuterBoundary = ReadPoints(reader); // always read
                data.InnerBoundary = ReadPoints(reader); // always read

                var rawRaceline = ReadPoints(reader);    // always read from file
                if (includeRaceline)
                    data.Raceline = rawRaceline;

                // Playerline will only exist in new-format files
                if (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var rawPlayerline = ReadPoints(reader);
                    if (includePlayerline)
                        data.Playerline = rawPlayerline;
                }

                if (includeCorners && reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int cornerCount = reader.ReadInt32();

                    if (cornerCount > 0)
                        data.Corners = ReadCorners(reader, cornerCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading binary file: {ex.Message}");
            }

            return data;
        }

        private static List<Point> ReadPoints(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var points = new List<Point>(count);
            for (int i = 0; i < count; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                points.Add(new Point((int)Math.Round(x), (int)Math.Round(y)));
            }
            return points;
        }

        private static List<CornerSegment> ReadCorners(BinaryReader reader, int count)
        {
            var corners = new List<CornerSegment>(count);
            for (int i = 0; i < count; i++)
            {
                float ix0 = reader.ReadSingle(), iy0 = reader.ReadSingle();
                float ix1 = reader.ReadSingle(), iy1 = reader.ReadSingle();
                float ox0 = reader.ReadSingle(), oy0 = reader.ReadSingle();
                float ox1 = reader.ReadSingle(), oy1 = reader.ReadSingle();
                float angle = reader.ReadSingle();
                bool isLeft = reader.ReadBoolean();
                int startIdx = reader.ReadInt32();
                int endIdx = reader.ReadInt32();

                corners.Add(new CornerSegment
                {
                    InnerStart = new Point((int)Math.Round(ix0), (int)Math.Round(iy0)),
                    InnerEnd = new Point((int)Math.Round(ix1), (int)Math.Round(iy1)),
                    OuterStart = new Point((int)Math.Round(ox0), (int)Math.Round(oy0)),
                    OuterEnd = new Point((int)Math.Round(ox1), (int)Math.Round(oy1)),
                    Angle = angle,
                    IsLeftTurn = isLeft,
                    StartIndex = startIdx,
                    EndIndex = endIdx,
                });
            }
            return corners;
        }

        private static List<Point> TransformPoints(List<Point> points, RectangleF bounds, Size canvasSize, float margin = 10f)
        {
            float scaleX = (canvasSize.Width - 2 * margin) / bounds.Width;
            float scaleY = (canvasSize.Height - 2 * margin) / bounds.Height;
            float scale = Math.Min(scaleX, scaleY); // Uniform scaling

            List<Point> transformed = new(points.Count);
            foreach (var pt in points)
            {
                float x = (pt.X - bounds.Left) * scale + margin;
                float y = (pt.Y - bounds.Top) * scale + margin;

                transformed.Add(new Point((int)x, (int)y));
            }
            return transformed;
        }

        private static Color InterpolateColor(Color startColor, Color endColor, float ratio)
        {
            ratio = Math.Max(0f, Math.Min(1f, ratio));

            int r = (int)(startColor.R + (endColor.R - startColor.R) * ratio);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * ratio);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * ratio);

            return Color.FromArgb(r, g, b);
        }

        private static void DrawContour(Graphics g, List<Point> points, Color color, bool closed = true, int thickness = 2)
        {
            if (points.Count < 2) return;

            using Pen pen = new(color, thickness);
            for (int i = 1; i < points.Count; i++)
                g.DrawLine(pen, points[i - 1], points[i]);

            if (closed && points.Count > 2)
                g.DrawLine(pen, points[^1], points[0]);
        }

        private static void DrawRacelineWithStartGradient(Graphics g, List<Point> points, int gradientSegments = 60, int thickness = 2)
        {
            if (points.Count < 2) return;

            int segmentsToDraw = Math.Min(gradientSegments, points.Count - 1);
            for (int i = 0; i < segmentsToDraw; i++)
            {
                Point start = points[i];
                Point end = points[i + 1];

                float ratio = (float)i / Math.Max(1, segmentsToDraw - 1);
                Color segmentColor = InterpolateColor(Color.Cyan, Color.LimeGreen, ratio);

                using Pen pen = new(segmentColor, thickness);
                g.DrawLine(pen, start, end);
            }

            if (points.Count > gradientSegments + 1)
            {
                using Pen greenPen = new(Color.Green, thickness);
                for (int i = gradientSegments; i < points.Count - 1; i++)
                {
                    g.DrawLine(greenPen, points[i], points[i + 1]);
                }
            }
        }

        private static void DrawLegend(Graphics g, Size canvasSize)
        {
            int legendWidth = 150;
            int legendHeight = 20;
            int margin = 10;

            Rectangle legendRect = new Rectangle(
                canvasSize.Width - legendWidth - margin,
                margin,
                legendWidth,
                legendHeight
            );

            using LinearGradientBrush gradientBrush = new LinearGradientBrush(
                legendRect,
                Color.Cyan,
                Color.LimeGreen,
                LinearGradientMode.Horizontal
            );

            g.FillRectangle(gradientBrush, legendRect);
            g.DrawRectangle(Pens.Black, legendRect);

            using Font font = new("Arial", 8);
            using Brush textBrush = new SolidBrush(Color.Black);

            g.DrawString("START", font, textBrush, legendRect.Left, legendRect.Bottom + 2);
            g.DrawString("Direction →", font, textBrush, legendRect.Right - 60, legendRect.Bottom + 2);
        }

        public static void DrawEdgesToImage(
            string binPath,
            string outputPath,
            Size canvasSize,
            bool includeRaceline = false,
            bool includeCorners = true,
            bool showDirectionGradient = true,
            bool includePlayerline = false) // NEW
        {
            EdgeData edges = ReadEdgesFromBin(binPath, includeRaceline, includeCorners, includePlayerline);

            var allPoints = edges.OuterBoundary.Concat(edges.InnerBoundary);
            if (includeRaceline) allPoints = allPoints.Concat(edges.Raceline);
            if (includePlayerline) allPoints = allPoints.Concat(edges.Playerline);

            RectangleF bounds = new(
                allPoints.Min(p => p.X),
                allPoints.Min(p => p.Y),
                allPoints.Max(p => p.X) - allPoints.Min(p => p.X),
                allPoints.Max(p => p.Y) - allPoints.Min(p => p.Y)
            );

            edges.OuterBoundary = TransformPoints(edges.OuterBoundary, bounds, canvasSize);
            edges.InnerBoundary = TransformPoints(edges.InnerBoundary, bounds, canvasSize);
            if (includeRaceline) edges.Raceline = TransformPoints(edges.Raceline, bounds, canvasSize);
            if (includePlayerline) edges.Playerline = TransformPoints(edges.Playerline, bounds, canvasSize);

            if (includeCorners)
            {
                for (int i = 0; i < edges.Corners.Count; i++)
                {
                    var c = edges.Corners[i];
                    edges.Corners[i] = new CornerSegment
                    {
                        InnerStart = TransformPoints(new List<Point> { c.InnerStart }, bounds, canvasSize)[0],
                        InnerEnd = TransformPoints(new List<Point> { c.InnerEnd }, bounds, canvasSize)[0],
                        OuterStart = TransformPoints(new List<Point> { c.OuterStart }, bounds, canvasSize)[0],
                        OuterEnd = TransformPoints(new List<Point> { c.OuterEnd }, bounds, canvasSize)[0],
                        Angle = c.Angle,
                        IsLeftTurn = c.IsLeftTurn,
                        StartIndex = c.StartIndex,
                        EndIndex = c.EndIndex,
                    };
                }
            }

            using Bitmap bitmap = new(canvasSize.Width, canvasSize.Height);
            using Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawContour(g, edges.OuterBoundary, Color.Blue);
            DrawContour(g, edges.InnerBoundary, Color.Red);

            if (includeRaceline && edges.Raceline.Count > 1)
            {
                if (showDirectionGradient)
                    DrawRacelineWithStartGradient(g, edges.Raceline, gradientSegments: 100, thickness: 3);
                else
                    DrawContour(g, edges.Raceline, Color.Green, closed: false);
            }

            if (includePlayerline && edges.Playerline.Count > 1)
            {
                // Magenta so it's distinct
                 DrawContour(g, edges.Playerline, Color.Magenta, closed: false, thickness: 2);
            }
            
            if (includeCorners && edges.Corners.Count > 0)
      {
        using Pen cornerPen = new(Color.Purple, 2);
        foreach (var corner in edges.Corners)
        {
          g.DrawLine(cornerPen, corner.InnerStart, corner.InnerEnd);
          g.DrawLine(cornerPen, corner.OuterStart, corner.OuterEnd);
        }
      }

            if (showDirectionGradient && includeRaceline && edges.Raceline.Count > 1)
            {
                DrawLegend(g, canvasSize);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            bitmap.Save(outputPath, ImageFormat.Png);
            Console.WriteLine($"Saved debug image to: {outputPath}");
        }
    }
}
