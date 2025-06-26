public class Particle
{
    public float[] Position;
    public float[] Velocity;
    public float[] BestPosition;
    public float BestCost;
    public int NoImprovementSteps = 0;

    public Particle(int dimensions, Random rand)
    {
        Position = new float[dimensions];
        Velocity = new float[dimensions];
        BestPosition = new float[dimensions];

        for (int i = 0; i < dimensions; i++)
        {
            Position[i] = (float)rand.NextDouble();
            Velocity[i] = 0.1f * ((float)rand.NextDouble() - 0.5f);
            BestPosition[i] = Position[i];
        }

        BestCost = float.MaxValue;
    }

    public void Randomize(Random rand)
    {
        for (int i = 0; i < Position.Length; i++)
        {
            Position[i] = (float)rand.NextDouble();
            Velocity[i] = 0.1f * ((float)rand.NextDouble() - 0.5f);
            BestPosition[i] = Position[i];
        }
        BestCost = float.MaxValue;
        NoImprovementSteps = 0;
    }
}
