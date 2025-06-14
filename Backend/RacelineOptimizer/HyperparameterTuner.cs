using System.Numerics;

namespace RacelineOptimizer
{
    public class HyperparameterTuner
    {
        private readonly List<(Vector2 inner, Vector2 outer)> track;
        private readonly List<CornerDetector.CornerSegment> corners;
        private readonly int trials;
        private readonly int seed;

        public HyperparameterTuner(List<(Vector2 inner, Vector2 outer)> track,
                                   List<CornerDetector.CornerSegment> corners,
                                   int trials = 50,
                                   int seed = 42)
        {
            this.track = track;
            this.corners = corners;
            this.trials = trials;
            this.seed = seed;
        }

        public void RunTuning()
        {
            Random rand = new Random(seed);
            float bestCost = float.MaxValue;
            float[] bestRatios = null;
            (float inertiaStart, float inertiaEnd, float cognitiveWeight, float socialWeight, float maxVelocity, int patience) bestParams = default;

            for (int i = 0; i < trials; i++)
            {
                // Sample random parameters
                float inertiaStart = Lerp(0.5f, 1.0f, (float)rand.NextDouble());
                float inertiaEnd = Lerp(0.1f, 0.6f, (float)rand.NextDouble());
                float cognitiveWeight = Lerp(0.5f, 3.0f, (float)rand.NextDouble());
                float socialWeight = Lerp(0.5f, 3.0f, (float)rand.NextDouble());
                float maxVelocity = Lerp(0.001f, 0.05f, (float)rand.NextDouble());
                int patience = rand.Next(300, 1501);

                // Run PSO with those parameters
                var pso = new PSO(
                    smoothnessWeight: 60f,
                    distanceWeight: 0f,
                    racingBiasWeight: 0.5f,
                    inertiaStart: inertiaStart,
                    inertiaEnd: inertiaEnd,
                    cognitiveWeight: cognitiveWeight,
                    socialWeight: socialWeight,
                    maxVelocity: maxVelocity,
                    patience: patience
                );

                float[] result = pso.Optimize(track, corners, numParticles: 30, iterations: 500);
                List<Vector2> raceline = pso.GenerateRaceline(track, result);
                float cost = GetTotalCost(pso, track, result, corners);

                Console.WriteLine($"Trial {i + 1}: Cost={cost:F4} | InertiaStart={inertiaStart:F2}, InertiaEnd={inertiaEnd:F2}, Cog={cognitiveWeight:F2}, Soc={socialWeight:F2}, MaxVel={maxVelocity:F4}, Pat={patience}");

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestRatios = result;
                    bestParams = (inertiaStart, inertiaEnd, cognitiveWeight, socialWeight, maxVelocity, patience);
                }
            }

            Console.WriteLine($"\nBest cost: {bestCost:F4}");
            Console.WriteLine($"Best parameters:");
            Console.WriteLine($"  InertiaStart: {bestParams.inertiaStart}");
            Console.WriteLine($"  InertiaEnd:   {bestParams.inertiaEnd}");
            Console.WriteLine($"  Cognitive:    {bestParams.cognitiveWeight}");
            Console.WriteLine($"  Social:       {bestParams.socialWeight}");
            Console.WriteLine($"  MaxVelocity:  {bestParams.maxVelocity}");
            Console.WriteLine($"  Patience:     {bestParams.patience}");
        }

        private float GetTotalCost(PSO pso, List<(Vector2 inner, Vector2 outer)> track, float[] ratios, List<CornerDetector.CornerSegment> corners)
        {
            // Unfortunately, EvaluateCost is private, so we repeat logic here
            var path = new List<Vector2>();
            for (int i = 0; i < track.Count; i++)
                path.Add(Vector2.Lerp(track[i].inner, track[i].outer, ratios[i]));

            float cornerCost = GetCorneringCost(track, corners, ratios);
            float smoothnessCost = GetSmoothnessCost(path);
            return cornerCost * 0.6f + smoothnessCost * 60f;
        }

        private float GetCorneringCost(List<(Vector2 inner, Vector2 outer)> track, List<CornerDetector.CornerSegment> corners, float[] ratios)
        {
            float cost = 0f;
            for (int i = 0; i < track.Count; i++)
            {
                float idealBias = GetCornerBias(corners, i);
                float ratio = ratios[i];

                if (idealBias > 0.5f)
                {
                    cost += (ratio > 0.5f) ? (1.0f - ratio) * 20 : (1.0f - ratio) * 100;
                }
                else if (idealBias < 0.5f)
                {
                    cost += (ratio < 0.5f) ? ratio * 10 : ratio * 1000;
                }
            }
            return cost;
        }

        private float GetSmoothnessCost(List<Vector2> path)
        {
            float totalCost = 0f;
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 v1 = Vector2.Normalize(path[i] - path[i - 1]);
                Vector2 v2 = Vector2.Normalize(path[i + 1] - path[i]);
                float dot = Vector2.Dot(v1, v2);
                dot = Math.Clamp(dot, -1f, 1f);
                float angle = MathF.Acos(dot);
                float curvature = angle / Vector2.Distance(path[i - 1], path[i + 1]);
                totalCost += curvature * curvature;
            }
            return totalCost * 100f;
        }

        private float GetCornerBias(List<CornerDetector.CornerSegment> corners, int index)
        {
            foreach (var corner in corners)
            {
                if (corner.EndIndex < index) continue;
                float severity = MathF.Min(1f, MathF.Abs(corner.Angle) / 90f);
                float t = Math.Clamp((index - corner.StartIndex) / (float)(corner.EndIndex - corner.StartIndex), 0f, 1f);
                float baseBias = MathF.Cos(t * MathF.PI);
                float biasOffset = baseBias * 0.5f * severity;
                return corner.IsLeftTurn ? 0.5f - biasOffset : 0.5f + biasOffset;
            }
            return 0.5f;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
