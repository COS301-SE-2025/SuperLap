using System.Numerics;

namespace RacelineOptimizer
{
    public static class TrackSampler
    {
        public static List<(Vector2 inner, Vector2 outer)> Sample(List<Vector2> inner, List<Vector2> outer, int numSamples, bool closed = true)
        {
            // First, uniformly resample the inner boundary
            List<Vector2> resampledInner = Resample(inner, numSamples, closed);
            
            // Then find corresponding outer points based on closest segments
            List<Vector2> matchedOuter = FindClosestOuterPoints(resampledInner, outer, closed);
            
            var result = new List<(Vector2, Vector2)>();
            for (int i = 0; i < numSamples; i++)
                result.Add((resampledInner[i], matchedOuter[i]));

            return result;
        }

        private static List<Vector2> FindClosestOuterPoints(List<Vector2> innerPoints, List<Vector2> outerPath, bool closed)
        {
            var matchedOuter = new List<Vector2>();
            
            foreach (var innerPoint in innerPoints)
            {
                Vector2 closestPoint = FindClosestPointOnPath(innerPoint, outerPath, closed);
                matchedOuter.Add(closestPoint);
            }
            
            return matchedOuter;
        }

        private static Vector2 FindClosestPointOnPath(Vector2 targetPoint, List<Vector2> path, bool closed)
        {
            float minDistance = float.MaxValue;
            Vector2 closestPoint = path[0];
            
            // Check each segment of the path
            int pathCount = closed ? path.Count : path.Count - 1;
            
            for (int i = 0; i < pathCount; i++)
            {
                Vector2 segmentStart = path[i];
                Vector2 segmentEnd = path[(i + 1) % path.Count];
                
                Vector2 pointOnSegment = GetClosestPointOnSegment(targetPoint, segmentStart, segmentEnd);
                float distance = Vector2.Distance(targetPoint, pointOnSegment);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = pointOnSegment;
                }
            }
            
            return closestPoint;
        }

        private static Vector2 GetClosestPointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segmentVector = segmentEnd - segmentStart;
            Vector2 pointVector = point - segmentStart;
            
            float segmentLengthSquared = segmentVector.LengthSquared();
            
            // If the segment is effectively a point, return the start point
            if (segmentLengthSquared < 1e-6f)
                return segmentStart;
            
            // Calculate the projection parameter t
            float t = Vector2.Dot(pointVector, segmentVector) / segmentLengthSquared;
            
            // Clamp t to [0, 1] to stay within the segment
            t = Math.Max(0f, Math.Min(1f, t));
            
            // Return the point on the segment
            return segmentStart + t * segmentVector;
        }

        private static List<Vector2> Resample(List<Vector2> path, int numSamples, bool closed = false)
        {
            List<Vector2> resampled = new();
            float totalLength = GetTotalLength(path, closed);
            float segmentLength = totalLength / (numSamples - 1);

            int pathIndex = 0;
            float distanceCovered = 0f;

            resampled.Add(path[0]);

            for (int i = 1; i < numSamples - 1; i++)
            {
                float targetDistance = i * segmentLength;

                while (true)
                {
                    int nextIndex = (pathIndex + 1) % path.Count;
                    float segment = Vector2.Distance(path[pathIndex], path[nextIndex]);

                    if (distanceCovered + segment >= targetDistance || (!closed && nextIndex == 0))
                        break;

                    distanceCovered += segment;
                    pathIndex = nextIndex;

                    if (!closed && pathIndex + 1 >= path.Count)
                    {
                        resampled.Add(path[^1]);
                        while (resampled.Count < numSamples)
                            resampled.Add(path[^1]);
                        return resampled;
                    }
                }

                int nextIdx = (pathIndex + 1) % path.Count;
                float remaining = targetDistance - distanceCovered;
                float segDist = Vector2.Distance(path[pathIndex], path[nextIdx]);
                float t = remaining / segDist;

                Vector2 point = Vector2.Lerp(path[pathIndex], path[nextIdx], t);
                resampled.Add(point);
            }

            resampled.Add(closed ? path[0] : path[^1]);
            return resampled;
        }

        private static float GetTotalLength(List<Vector2> path, bool closed = false)
        {
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
                length += Vector2.Distance(path[i - 1], path[i]);

            if (closed)
                length += Vector2.Distance(path[^1], path[0]);

            return length;
        }
    }
}