using UnityEngine;

public class GPUPSO : MonoBehaviour
{
    public ComputeShader psoShader;
    public int numParticles = 1024;
    public int dimensions = 100;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer velocityBuffer;
    private ComputeBuffer bestCostBuffer;
    private ComputeBuffer bestPositionBuffer;

    void Start()
    {
        InitBuffers();
        DispatchCompute();
        ReadBackData();
        Debug.Log("GPUPSO Start() called");
    }

    public void ManualRun()
    {
        Debug.Log("GPUPSO ManualRun called");

        InitBuffers();
        DispatchCompute();
        ReadBackData();
    }

    void InitBuffers()
    {
        int total = numParticles * dimensions;
        positionBuffer = new ComputeBuffer(total, sizeof(float));
        velocityBuffer = new ComputeBuffer(total, sizeof(float));

        float[] initPositions = new float[total];
        float[] initVelocities = new float[total];

        System.Random rand = new();
        for (int i = 0; i < total; i++)
        {
            initPositions[i] = (float)rand.NextDouble();
            initVelocities[i] = 0f;
        }

        positionBuffer.SetData(initPositions);
        velocityBuffer.SetData(initVelocities);

        bestCostBuffer = new ComputeBuffer(1, sizeof(uint)); // changed from float to uint
        bestPositionBuffer = new ComputeBuffer(dimensions, sizeof(float));

        // Set initial best cost to a very high sortable uint
        uint[] initBestCost = { FloatToSortableUint(float.MaxValue) };
        float[] initBestPos = new float[dimensions]; // Empty initial position

        bestCostBuffer.SetData(initBestCost);
        bestPositionBuffer.SetData(initBestPos);
    }

    void DispatchCompute()
    {
        int kernel = psoShader.FindKernel("CSMain");

        psoShader.SetInt("numParticles", numParticles);
        psoShader.SetInt("dimensions", dimensions);

        psoShader.SetBuffer(kernel, "positions", positionBuffer);
        psoShader.SetBuffer(kernel, "velocities", velocityBuffer);

        psoShader.SetBuffer(kernel, "globalBestCost", bestCostBuffer);
        psoShader.SetBuffer(kernel, "globalBestPosition", bestPositionBuffer);

        int threadGroups = Mathf.CeilToInt(numParticles / 64f);
        psoShader.Dispatch(kernel, threadGroups, 1, 1);
    }

    void ReadBackData()
    {
        float[] results = new float[numParticles * dimensions];
        positionBuffer.GetData(results);

        uint[] raw = new uint[1];
        bestCostBuffer.GetData(raw);
        float bestCost = SortableUintToFloat(raw[0]);

        float[] bestPosition = new float[dimensions];
        bestPositionBuffer.GetData(bestPosition);

        Debug.Log($"Best cost from GPU: {bestCost}");
        Debug.Log($"Best position (first 3 dims): {bestPosition[0]}, {bestPosition[1]}, {bestPosition[2]}");

        Debug.Log($"First particle: {results[0]}, {results[1]}, {results[2]}");
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        velocityBuffer?.Release();
        bestCostBuffer?.Release();
        bestPositionBuffer?.Release();
    }

    // Conversion helpers
    private static uint FloatToSortableUint(float f)
    {
        uint i = (uint)System.BitConverter.SingleToInt32Bits(f);
        return (i & 0x80000000u) != 0 ? ~i : i ^ 0x80000000u;
    }

    private static float SortableUintToFloat(uint i)
    {
        int restored = (i & 0x80000000u) != 0 ? (int)(i ^ 0x80000000u) : ~((int)i);
        return System.BitConverter.Int32BitsToSingle(restored);
    }
}
