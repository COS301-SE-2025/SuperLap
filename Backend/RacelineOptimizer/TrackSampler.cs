using System.Numerics;

namespace RacelineOptimizer
{
    public static class TrackSampler
    {
        public static List<(Vector2 inner, Vector2 outer)> Sample(List<Vector2> inner, List<Vector2> outer, int numSamples, bool closed = true)
        {
            // First, uniformly resample the inner boundary
            List<Vector2> resampledInner = Resample(inner, numSamples, closed);

            return resampledInner;
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