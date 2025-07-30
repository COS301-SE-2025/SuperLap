using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class CornerDetector
{
    // Corner Detection Settings
    public static int AngleWindowSize = 6;             // Number of steps used to compute direction change (must be even)
    public static int AngleSmoothWindow = 3;           // Window size for smoothing the angle change signal
    public static float AngleChangeThreshold = 4f;   // Minimum angle change (in degrees) to consider part of a turn
    public static float NetCornerAngleThreshold = 30f; // Total angle (in degrees) needed to classify a corner
    public static float MinStartEndAngleChange = 20f; // Minimum angle change (in degrees) from start to end of corner
    public static int MinCornerSegments = 35;          // Minimum physical length (in meters) for a corner
    public static int MinCornerSegmentsCount = 40;     // Minimum number of data points that make up a corner
    public static int MaxCornerSegmentsCount = 2000; // Maximum number of data points allowed in a corner
    public static int FlatLimit = 10;                  // Max flat/no-turn segments allowed when extending a corner
    public static int CandidateWindowSize = 15;        // Initial window size to detect potential corner regions
    public static float CandidateAngleThreshold = 15f; // Minimum angle sum (in degrees) for candidate region
    public static int MergeGap = 5;                   // Max number of segments between candidates to allow merging
    public static int SmoothingWindow = 5;             // Window size for smoothing the centerline path
        
    public struct CornerSegment
    {
        public Vector2 InnerStart { get; }
        public Vector2 InnerEnd { get; }
        public Vector2 OuterStart { get; }
        public Vector2 OuterEnd { get; }
        public float Angle { get; }
        public bool IsLeftTurn { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
        public float Length { get; }

        public override string ToString() =>
            $"[{StartIndex}-{EndIndex}] {Angle:F1}° {(IsLeftTurn ? "Left" : "Right")} turn " +
            $"{Length:F1}m\nInner: ({InnerStart.X:F1},{InnerStart.Y:F1})→({InnerEnd.X:F1},{InnerEnd.Y:F1})\n" +
            $"Outer: ({OuterStart.X:F1},{OuterStart.Y:F1})→({OuterEnd.X:F1},{OuterEnd.Y:F1})";

        public CornerSegment(Vector2 innerStart, Vector2 innerEnd,
                            Vector2 outerStart, Vector2 outerEnd,
                            float Angle, bool isLeftTurn,
                            int startIndex, int endIndex, float length)
        {
            InnerStart = innerStart;
            InnerEnd = innerEnd;
            OuterStart = outerStart;
            OuterEnd = outerEnd;
            this.Angle = Angle;
            IsLeftTurn = isLeftTurn;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Length = length;
        }
    }

    public static List<CornerSegment> DetectCorners(List<(Vector2 inner, Vector2 outer)> track)
    {
        if (track == null || track.Count < 20)
            return new List<CornerSegment>();

        List<Vector2> centerline = GetCenterline(track);
        List<Vector2> smoothedCenterline = SmoothCenterline(centerline, SmoothingWindow);
        List<float> angleChanges = CalculateAngleChanges(smoothedCenterline);
        List<float> smoothedAngles = SmoothAngles(angleChanges, AngleSmoothWindow);
        return FindCornerRegions(track, smoothedCenterline, smoothedAngles);
    }

    private static List<float> CalculateAngleChanges(List<Vector2> centerline)
    {
        List<float> angleChanges = new List<float>();
        int half = AngleWindowSize / 2;

        for (int i = 0; i < centerline.Count; i++)
        {
            if (i < half || i >= centerline.Count - half)
            {
                angleChanges.Add(0f);
                continue;
            }

            Vector2 vec1 = Vector2.Normalize(centerline[i] - centerline[i - half]);
            Vector2 vec2 = Vector2.Normalize(centerline[i + half] - centerline[i]);

            float angleChange = ComputeSignedAngle(vec1, vec2);
            angleChanges.Add(angleChange);
        }

        return angleChanges;
    }

    private static List<float> SmoothAngles(List<float> angles, int windowSize)
    {
        List<float> smoothed = new List<float>();
        int half = windowSize / 2;

        for (int i = 0; i < angles.Count; i++)
        {
            float sum = 0f;
            int count = 0;

            for (int j = -half; j <= half; j++)
            {
                int idx = i + j;
                if (idx >= 0 && idx < angles.Count)
                {
                    sum += angles[idx];
                    count++;
                }
            }

            smoothed.Add(sum / count);
        }

        return smoothed;
    }

    private static List<CornerSegment> FindCornerRegions(
        List<(Vector2 inner, Vector2 outer)> track,
        List<Vector2> centerline,
        List<float> angleChanges)
    {
        List<CornerSegment> corners = new List<CornerSegment>();
        List<CornerCandidate> candidates = IdentifyCornerCandidates(angleChanges);
        List<CornerCandidate> mergedCandidates = MergeAndValidateCandidates(candidates, centerline, angleChanges);

        foreach (var candidate in mergedCandidates)
        {
            if (IsValidCorner(centerline, candidate.Start, candidate.End, candidate.TotalAngle))
            {
                float cornerLength = CalculateSegmentLength(centerline, candidate.Start, candidate.End);

                CornerSegment corner = new CornerSegment(
                    track[candidate.Start].inner,
                    track[candidate.End].inner,
                    track[candidate.Start].outer,
                    track[candidate.End].outer,
                    MathF.Abs(candidate.AngleChange),
                    !candidate.IsLeftTurn,
                    candidate.Start,
                    candidate.End,
                    cornerLength
                );
                corners.Add(corner);
            }
        }

        return corners;
    }

    private struct CornerCandidate
    {
        public int Start { get; set; }
        public int End { get; set; }
        public float TotalAngle { get; set; }
        public float AngleChange { get; set; }
        public bool IsLeftTurn { get; set; }

        public CornerCandidate(int start, int end, float totalAngle, float AngleChange, bool isLeftTurn)
        {
            Start = start;
            End = end;
            TotalAngle = totalAngle;
            this.AngleChange = AngleChange;
            IsLeftTurn = isLeftTurn;
        }
    }

    private static List<CornerCandidate> IdentifyCornerCandidates(List<float> angleChanges)
    {
        List<CornerCandidate> candidates = new List<CornerCandidate>();
        int windowSize = CandidateWindowSize;
        float threshold = CandidateAngleThreshold;

        for (int i = 0; i <= angleChanges.Count - windowSize; i++)
        {
            float windowAngle = 0f;
            for (int j = i; j < i + windowSize; j++)
                windowAngle += angleChanges[j];

            if (MathF.Abs(windowAngle) >= threshold)
            {
                bool isLeftTurn = windowAngle > 0;
                float AngleChange = windowAngle;
                int start = ExtendCornerStart(angleChanges, i, isLeftTurn);
                int end = ExtendCornerEnd(angleChanges, i + windowSize - 1, isLeftTurn);

                float totalAngle = 0f;
                for (int j = start; j <= end && j < angleChanges.Count; j++)
                {
                    if ((angleChanges[j] > 0) == isLeftTurn || MathF.Abs(angleChanges[j]) < 1f)
                        totalAngle += angleChanges[j];
                }

                candidates.Add(new CornerCandidate(start, end, totalAngle, AngleChange, isLeftTurn));
                i = end;
            }
        }

        return candidates;
    }

    private static int ExtendCornerStart(List<float> angleChanges, int start, bool isLeftTurn)
    {
        int extended = start;
        int flats = 0;

        for (int i = start - 1; i >= 0; i--)
        {
            float angle = angleChanges[i];
            bool sameDirection = angle > 0 == isLeftTurn;

            if (MathF.Abs(angle) >= 2f)
            {
                if (sameDirection)
                {
                    extended = i;
                    flats = 0;
                }
                else if (MathF.Abs(angle) >= AngleChangeThreshold)
                    break;
            }
            else
            {
                flats++;
                if (flats >= FlatLimit) break;
            }
        }

        return extended;
    }

    private static int ExtendCornerEnd(List<float> angleChanges, int end, bool isLeftTurn)
    {
        int extended = end;
        int flats = 0;

        for (int i = end + 1; i < angleChanges.Count; i++)
        {
            float angle = angleChanges[i];
            bool sameDirection = angle > 0 == isLeftTurn;

            if (MathF.Abs(angle) >= 2f)
            {
                if (sameDirection)
                {
                    extended = i;
                    flats = 0;
                }
                else if (MathF.Abs(angle) >= AngleChangeThreshold)
                    break;
            }
            else
            {
                flats++;
                if (flats >= FlatLimit) break;
            }
        }

        return extended;
    }

    private static List<CornerCandidate> MergeAndValidateCandidates(
        List<CornerCandidate> candidates,
        List<Vector2> centerline,
        List<float> angleChanges)
    {
        if (candidates.Count == 0) return candidates;

        candidates.Sort((a, b) => a.Start.CompareTo(b.Start));
        List<CornerCandidate> merged = new List<CornerCandidate>();

        CornerCandidate current = candidates[0];

        for (int i = 1; i < candidates.Count; i++)
        {
            CornerCandidate next = candidates[i];
            int gap = next.Start - current.End;

            if (gap <= MergeGap && current.IsLeftTurn == next.IsLeftTurn)
            {
                float combinedAngle = current.TotalAngle;
                for (int j = current.End + 1; j <= next.End && j < angleChanges.Count; j++)
                {
                    if ((angleChanges[j] > 0) == current.IsLeftTurn || MathF.Abs(angleChanges[j]) < 1f)
                        combinedAngle += angleChanges[j];
                }

                current = new CornerCandidate(current.Start, next.End, combinedAngle, current.AngleChange, current.IsLeftTurn);
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return merged;
    }

    

    private static float ComputeSignedAngle(Vector2 from, Vector2 to)
    {
        float dot = Vector2.Dot(from, to);
        float det = from.X * to.Y - from.Y * to.X;
        return MathF.Atan2(det, dot) * (180f / MathF.PI);
    }

    private static float CalculateSegmentLength(List<Vector2> points, int start, int end)
    {
        float length = 0f;
        for (int i = start; i < end; i++)
            length += Vector2.Distance(points[i], points[i + 1]);
        return length;
    }
}