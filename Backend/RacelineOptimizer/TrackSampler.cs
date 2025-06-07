using System.Collections.Generic;
using System.Numerics;

namespace RacelineOptimizer
{
    public static class TrackSampler
    {
        public static List<(Vector2 inner, Vector2 outer)> Sample(List<Vector2> inner, List<Vector2> outer, int numSamples)
        {
            var sampled = new List<(Vector2, Vector2)>();
            int stepInner = inner.Count / numSamples;
            int stepOuter = outer.Count / numSamples;

            for (int i = 0; i < numSamples; i++)
            {
                var innerPoint = inner[i * stepInner];
                var outerPoint = outer[i * stepOuter];
                sampled.Add((innerPoint, outerPoint));
            }

            return sampled;
        }
    }
}
