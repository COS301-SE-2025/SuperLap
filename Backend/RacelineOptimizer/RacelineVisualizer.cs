using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RacelineVisualizer
{
    public class EdgeDataVisualizer
    {
        public class EdgeData
        {
            public List<Point> OuterBoundary { get; set; } = new();
            public List<Point> InnerBoundary { get; set; } = new();
            public List<Point> Raceline { get; set; } = new();
            public List<Point> Playerline { get; set; } = new(); // ✅ New playerline support
            public List<CornerSegment> Corners { get; set; } = new();
        }

        public struct CornerSegment
        {
            public Point InnerStart;
            public Point InnerEnd;
            public Point OuterStart;
            public Point OuterEnd;
        }

        public static EdgeData ReadEdgesFromBin(string path, bool includeRaceline = false, bool includeCorners = false, bool includePlayerline = false)
        {
            EdgeData data = new();

            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                data.OuterBoundary = ReadPoints(reader);
                data.InnerBoundary = ReadPoints(reader);

                if (includeRaceline && reader.BaseStream.Position < reader.BaseStream.Length)
                    data.Raceline = ReadPoints(reader);

                if (includeCorners && reader.BaseStream.Position < reader.BaseStream.Length)
                    data.Corners = ReadCorners(reader);

                if (includePlayerline && reader.BaseStream.Position < reader.BaseStream.Length)
                    data.Playerline = ReadPoints(reader); // ✅
            }

            return data;
        }

        private static void DrawGrid(Graphics g, Size canvasSize, RectangleF dataBounds, float gridSpacing = 50f, Color? gridColor = null)
        {
            Color color = gridColor ?? Color.LightGray;
            using Pen pen = new(color, 1);
            pen.DashStyle = DashStyle.Dot;

            using Font font = new Font("Arial", 8);
            using Brush textBrush = new SolidBrush(Color.Gray);

            // Helper to convert data point to pixel point (same as before)
            Point DataToPixel(float x, float y)
            {
                float scaleX = (canvasSize.Width - 20) / dataBounds.Width;  // 10px margin each side
                float scaleY = (canvasSize.Height - 30) / dataBounds.Height; // Add extra top margin (30)
                float scale = Math.Min(scaleX, scaleY);

                float px = (x - dataBounds.Left) * scale + 10;
                float py = (y - dataBounds.Top) * scale + 30; // shift down by 30 to leave top margin
                return new Point((int)px, (int)py);
            }

            // Draw vertical grid lines and rotated labels
            for (float x = (float)(Math.Floor(dataBounds.Left / gridSpacing) * gridSpacing); x <= dataBounds.Right; x += gridSpacing)
            {
                Point p1 = DataToPixel(x, dataBounds.Top);
                Point p2 = DataToPixel(x, dataBounds.Bottom);
                g.DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);

                string label = x.ToString("0.##");
                SizeF size = g.MeasureString(label, font);

                g.TranslateTransform(p1.X, p1.Y - 5); // position for text: slightly above the top line
                g.RotateTransform(-45); // rotate text

                // Draw text with offset to center it after rotation
                g.DrawString(label, font, textBrush, -size.Width / 2, -size.Height / 2);

                g.ResetTransform();
            }

            // Draw horizontal grid lines and labels (no rotation)
            for (float y = (float)(Math.Floor(dataBounds.Top / gridSpacing) * gridSpacing); y <= dataBounds.Bottom; y += gridSpacing)
            {
                Point p1 = DataToPixel(dataBounds.Left, y);
                Point p2 = DataToPixel(dataBounds.Right, y);
                g.DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);

                string label = y.ToString("0.##");
                SizeF size = g.MeasureString(label, font);

                // Draw label slightly right of left edge, centered vertically on the line
                g.DrawString(label, font, textBrush, p1.X + 2, p1.Y - size.Height / 2);
            }
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

        private static List<CornerSegment> ReadCorners(BinaryReader reader)
        {
            int count = reader.ReadInt32();
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

            using Font font = new Font("Arial", 8);
            using Brush textBrush = new SolidBrush(Color.Black);

            g.DrawString("START", font, textBrush, legendRect.Left, legendRect.Bottom + 2);
            g.DrawString("Direction →", font, textBrush, legendRect.Right - 60, legendRect.Bottom + 2);
        }

        public static void DrawEdgesToImage(string binPath, string outputPath, Size canvasSize, bool includeRaceline = false, bool includeCorners = true, bool showDirectionGradient = true, bool includePlayerline = false)
        {
            EdgeData edges = ReadEdgesFromBin(binPath, includeRaceline, includeCorners, includePlayerline);

            var allPoints = edges.OuterBoundary.Concat(edges.InnerBoundary);
            if (includeRaceline) allPoints = allPoints.Concat(edges.Raceline);
            if (includePlayerline) allPoints = allPoints.Concat(edges.Playerline);

            var allXs = allPoints.Select(p => p.X);
            var allYs = allPoints.Select(p => p.Y);

            RectangleF bounds = new(
                allXs.Min(),
                allYs.Min(),
                allXs.Max() - allXs.Min(),
                allYs.Max() - allYs.Min()
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
                        InnerStart = TransformPoints([c.InnerStart], bounds, canvasSize)[0],
                        InnerEnd = TransformPoints([c.InnerEnd], bounds, canvasSize)[0],
                        OuterStart = TransformPoints([c.OuterStart], bounds, canvasSize)[0],
                        OuterEnd = TransformPoints([c.OuterEnd], bounds, canvasSize)[0],
                    };
                }
            }

            using Bitmap bitmap = new(canvasSize.Width, canvasSize.Height);
            using Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            DrawGrid(g, canvasSize, bounds, 50f);
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
                using Pen playerPen = new(Color.Magenta, 2);
                for (int i = 1; i < edges.Playerline.Count; i++)
                    g.DrawLine(playerPen, edges.Playerline[i - 1], edges.Playerline[i]);
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
