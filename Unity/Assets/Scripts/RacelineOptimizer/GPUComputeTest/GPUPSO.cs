using UnityEngine;

public class GPUPSO : MonoBehaviour
{
    public ComputeShader psoShader;
    public int numParticles = 1024;
    public int dimensions = 100;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer velocityBuffer;

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
    }

    void DispatchCompute()
    {
        int kernel = psoShader.FindKernel("CSMain");

        psoShader.SetInt("numParticles", numParticles);
        psoShader.SetInt("dimensions", dimensions);

        psoShader.SetBuffer(kernel, "positions", positionBuffer);
        psoShader.SetBuffer(kernel, "velocities", velocityBuffer);

        int threadGroups = Mathf.CeilToInt(numParticles / 64f);
        psoShader.Dispatch(kernel, threadGroups, 1, 1);
    }

    void ReadBackData()
    {
        float[] results = new float[numParticles * dimensions];
        positionBuffer.GetData(results);

        // Example debug: print first particle's first few dimensions
        Debug.Log($"First particle: {results[0]}, {results[1]}, {results[2]}");
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        velocityBuffer?.Release();
    }
}
