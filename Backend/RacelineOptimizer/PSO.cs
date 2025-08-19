using System.Numerics;

namespace RacelineOptimizer
{
    public class PSO
    {
        private readonly float smoothnessWeight;
        private readonly float distanceWeight;
        private readonly float racingBiasWeight;
        private readonly float inertiaStart;
        private readonly float inertiaEnd;
        private readonly float cognitiveWeight;
        private readonly float socialWeight;
        private readonly float maxVelocity;
        private int patience;

        public PSO(
            float smoothnessWeight = 10f, //Favours less sharp turns (25f)
            float distanceWeight = 0.0f, //Favours shorter paths(typically more straight) (4f)
            float racingBiasWeight = 0.21f,  //Favours paths opposite to upcoming corner direction.
            float inertiaStart = 0.6f,
            float inertiaEnd = 0.1f,
            float cognitiveWeight = 1.7f,
            float socialWeight = 1.7f,
            float maxVelocity = 0.01f,
            int patience = 1000
        )
        {
            this.smoothnessWeight = smoothnessWeight;
            this.distanceWeight = distanceWeight;
            this.racingBiasWeight = racingBiasWeight;
            this.inertiaStart = inertiaStart;
            this.inertiaEnd = inertiaEnd;
            this.cognitiveWeight = cognitiveWeight;
            this.socialWeight = socialWeight;
            this.maxVelocity = maxVelocity;
            this.patience = patience;
        }

        private static float Clamp(float value, float min, float max)
        {
            return MathF.Max(min, MathF.Min(max, value));
        }

        private static float GetCornerBias(
            List<CornerDetector.CornerSegment> corners,
            int index,
            int currentTrackLength,
            int cornerTrackLength)
        {
            // Remap the index from the current track to cornerTrack space
            int mappedIndex = (int)(index * (cornerTrackLength / (float)currentTrackLength));

            foreach (var corner in corners)
            {
                if (corner.EndIndex < mappedIndex)
                    continue;

                float severity = MathF.Min(1f, MathF.Abs(corner.Angle) / 90f);
                float t = Math.Clamp((mappedIndex - corner.StartIndex) / (float)(corner.EndIndex - corner.StartIndex), 0f, 1f);

                // Jump immediately to ideal bias at entry (cosine step removed)
                float baseBias = (mappedIndex < corner.StartIndex + (corner.EndIndex - corner.StartIndex) / 3f) ? -1f :
                                (mappedIndex > corner.StartIndex + 2 * (corner.EndIndex - corner.StartIndex) / 3f) ? 1f :
                                0f;

                float biasOffset = baseBias * 0.5f * severity;

                return corner.IsLeftTurn
                    ? 0.5f - biasOffset
                    : 0.5f + biasOffset;
            }

            return 0.5f;
        }

        
        private float CalculateCorneringCost(List<(Vector2 inner, Vector2 outer)> track, List<CornerDetector.CornerSegment> corners,  float[] ratios, List<(Vector2 inner, Vector2 outer)> cornerTrack)
        {
            float cost = 0f;
            for (int i = 0; i < track.Count; i++)
            {
                float idealBias = GetCornerBias(corners, i, track.Count, cornerTrack.Count);


                if (idealBias > 0.5f)
                {
                    if (ratios[i] > 0.5f)
                    {
                        cost += (1.0f - ratios[i]) * 20;
                    }
                    else
                    {
                        cost += (1.0f - ratios[i]) * 100;
                    }
                }
                else if (idealBias < 0.5f)
                {
                    if (ratios[i] < 0.5f)
                    {
                        cost += ratios[i] * 10;
                    }
                    else
                    {
                        cost += ratios[i] * 1000;
                    }
                }
            }
            return cost;
        }


        private float CalculateSmoothnessCost(List<Vector2> path)
        {
            float totalCost = 0f;
            int count = path.Count;
            if (count < 3) return float.MaxValue;

            for (int i = 1; i < count - 1; i++)
            {
                Vector2 prev = path[i - 1];
                Vector2 curr = path[i];
                Vector2 next = path[i + 1];

                Vector2 v1 = Vector2.Normalize(curr - prev);
                Vector2 v2 = Vector2.Normalize(next - curr);

                float dot = Vector2.Dot(v1, v2);
                dot = Math.Clamp(dot, -1f, 1f);

                float angle = MathF.Acos(dot);

                float curvature = angle / Vector2.Distance(prev, next);
                totalCost += curvature * curvature;
            }

            return totalCost * 100000f;
        }

        private float EvaluateDistanceCost(List<Vector2> path)
        {
            float totalDistance = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                totalDistance += Vector2.Distance(path[i - 1], path[i]);
            }
            return totalDistance;
        }

        private float EvaluateCost(List<(Vector2 inner, Vector2 outer)> track, float[] ratios, List<CornerDetector.CornerSegment> corners, List<(Vector2 inner, Vector2 outer)> cornerTrack)
        {
            List<Vector2> path = new(track.Count);
            for (int i = 0; i < track.Count; i++)
                path.Add(Vector2.Lerp(track[i].inner, track[i].outer, ratios[i]));

            float cost = 0f;
            float corneringCost = CalculateCorneringCost(track, corners, ratios, cornerTrack);
            float smoothnessCost = CalculateSmoothnessCost(path);
            float distanceCost = EvaluateDistanceCost(path);
            cost += distanceCost * distanceWeight
                + corneringCost * racingBiasWeight
                + smoothnessCost * smoothnessWeight;

            return cost;
        }


        public float[] Optimize(List<(Vector2 inner, Vector2 outer)> track, List<CornerDetector.CornerSegment> corners, List<(Vector2 inner, Vector2 outer)> cornerTrack, int numParticles = 30, int iterations = 100)
        {
            object globalLock = new();
            ThreadLocal<Random> threadRand = new(() => new Random(Guid.NewGuid().GetHashCode()));
            int dimensions = track.Count;
            Random rand = new();
            var particles = new List<Particle>();
            float[] globalBest = new float[dimensions];
            float globalBestCost = float.MaxValue;

            float[] recentCosts = new float[patience];
            int recentIndex = 0;

            for (int i = 0; i < numParticles; i++)
            {
                var p = new Particle(dimensions, rand);
                p.Position[^1] = p.Position[0];
                p.BestPosition[^1] = p.BestPosition[0];

                p.BestCost = EvaluateCost(track, p.Position, corners, cornerTrack);
                if (p.BestCost < globalBestCost)
                {
                    globalBestCost = p.BestCost;
                    Array.Copy(p.Position, globalBest, dimensions);
                }
                particles.Add(p);
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                float inertiaWeight = inertiaStart + (inertiaEnd - inertiaStart) * (iter / (float)iterations);

                Parallel.ForEach(particles, () => new Random(Guid.NewGuid().GetHashCode()), (p, _, localRand) =>
                {
                    for (int d = 0; d < dimensions; d++)
                    {
                        float inertia = inertiaWeight * p.Velocity[d];
                        float cognitive = cognitiveWeight * (float)localRand.NextDouble() * (p.BestPosition[d] - p.Position[d]);
                        float social = socialWeight * (float)localRand.NextDouble() * (globalBest[d] - p.Position[d]);

                        p.Velocity[d] = inertia + cognitive + social;
                        p.Velocity[d] = Clamp(p.Velocity[d], -maxVelocity, maxVelocity);

                        p.Position[d] += p.Velocity[d];
                        p.Position[d] = Clamp(p.Position[d], 0f, 1f);
                    }
                    p.Position[^1] = p.Position[0]; // Ensure loop closure

                    float cost = EvaluateCost(track, p.Position, corners, cornerTrack);
                    if (cost < p.BestCost)
                    {
                        p.BestCost = cost;
                        Array.Copy(p.Position, p.BestPosition, dimensions);
                        p.NoImprovementSteps = 0;

                        lock (globalLock)
                        {
                            if (cost < globalBestCost)
                            {
                                globalBestCost = cost;
                                Array.Copy(p.Position, globalBest, dimensions);
                            }
                        }
                    }
                    else
                    {
                        p.NoImprovementSteps++;
                    }

                    return localRand;
                }, _ => { });

                // Begin anti-local-minimum enhancements
                int resetInterval = 200;
                float perturbAmount = 0.05f;
                int stagnationLimit = 50;

                if (iter % resetInterval == 0)
                {
                    Particle worst = particles.OrderByDescending(p => p.BestCost).First();
                    lock (globalLock)
                    {
                        worst.Randomize(rand);
                        worst.Position[^1] = worst.Position[0];
                        worst.BestPosition[^1] = worst.BestPosition[0];
                        worst.BestCost = EvaluateCost(track, worst.Position, corners, cornerTrack);
                        if (worst.BestCost < globalBestCost)
                        {
                            globalBestCost = worst.BestCost;
                            Array.Copy(worst.Position, globalBest, dimensions);
                        }
                    }
                }

                // Apply perturbation to stuck particles
                foreach (var p in particles)
                {
                    if (p.NoImprovementSteps >= stagnationLimit)
                    {
                        for (int d = 0; d < dimensions; d++)
                        {
                            float perturb = perturbAmount * ((float)rand.NextDouble() - 0.5f);
                            p.Position[d] = Clamp(p.Position[d] + perturb, 0f, 1f);
                        }
                        p.Position[^1] = p.Position[0];
                        p.NoImprovementSteps = 0;
                    }
                }

                // Store recent cost improvement
                recentCosts[recentIndex % patience] = globalBestCost;
                recentIndex++;

                // Check early stopping every iteration
                if (recentIndex >= patience)
                {
                    float oldest = recentCosts[(recentIndex - patience) % patience];
                    float improvement = MathF.Abs(oldest - globalBestCost);
                    if (improvement < 0.01f)
                    {
                        Console.WriteLine($"Early stopping at iteration {iter}, BestCost = {globalBestCost:F4}");
                        break;
                    }
                }

                if (iter % 1000 == 0)
                    Console.WriteLine($"Iteration {iter}, BestCost = {globalBestCost:F4}");
            }

            return globalBest;
        }


        public List<Vector2> SmoothRaceline(List<Vector2> raceline, int iterations = 2)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                List<Vector2> smoothed = new();
                int count = raceline.Count;

                for (int i = 0; i < count; i++)
                {
                    Vector2 p0 = raceline[i];
                    Vector2 p1 = raceline[(i + 1) % count];
                    smoothed.Add(Vector2.Lerp(p0, p1, 0.25f));
                    smoothed.Add(Vector2.Lerp(p0, p1, 0.75f));
                }

                raceline = smoothed;
            }

            return raceline;
        }

        public List<Vector2> ClampToTrack(List<(Vector2 inner, Vector2 outer)> track, List<Vector2> raceline)
        {
            List<Vector2> clamped = new();

            for (int i = 0; i < track.Count && i < raceline.Count; i++)
            {
                Vector2 A = track[i].inner;
                Vector2 B = track[i].outer;
                Vector2 P = raceline[i];

                // Check if P is between A and B (within track width at this point)
                Vector2 AB = B - A;
                Vector2 AP = P - A;

                float proj = Vector2.Dot(AP, AB) / Vector2.Dot(AB, AB);

                // Distance from point to line segment
                float t = Clamp(proj, 0f, 1f);
                Vector2 projection = Vector2.Lerp(A, B, t);

                // Check if point is "off track"
                float distToSegmentSq = (P - projection).LengthSquared();
                float trackWidthSq = (B - A).LengthSquared();

                // If outside narrow margin from segment line, clamp
                if (proj < 0f || proj > 1f || distToSegmentSq > 1e-4f * trackWidthSq)
                {
                    clamped.Add(projection);
                }
                else
                {
                    clamped.Add(P); // Keep original if already on track
                }
            }

            return clamped;
        }


        public List<Vector2> GenerateRaceline(List<(Vector2 inner, Vector2 outer)> track, float[] ratios)
        {
            var raceline = new List<Vector2>();
            for (int i = 0; i < track.Count; i++)
                raceline.Add(Vector2.Lerp(track[i].inner, track[i].outer, ratios[i]));

            if (raceline.Count > 1 && raceline[0] != raceline[^1])
                raceline.Add(raceline[0]);

            return raceline;
        }
    }
}