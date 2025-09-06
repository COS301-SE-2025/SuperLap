using System;
using System.Numerics;
// using UnityEngine;
using Vector2 = System.Numerics.Vector2;

/// <summary>
/// Double-buffered agent state storage for worker threads.
/// Each worker thread writes to one buffer while the main thread reads from the other.
/// </summary>
public class WorkerThreadBuffer
{
    private readonly object bufferSwapLock = new object();
    
    // Double buffers - worker writes to one, main thread reads from the other
    private AgentStateArray writeBuffer;
    private AgentStateArray readBuffer;
    
    // Synchronization flags
    private volatile bool dataReady = false;
    private volatile bool bufferSwapped = false;
    
    public bool DataReady => dataReady;
    public int Capacity { get; private set; }
    
    public WorkerThreadBuffer(int maxAgents)
    {
        Capacity = maxAgents;
        writeBuffer = new AgentStateArray(maxAgents);
        readBuffer = new AgentStateArray(maxAgents);
    }
    
    /// <summary>
    /// Worker thread: Get the buffer to write agent states to
    /// </summary>
    public AgentStateArray GetWriteBuffer()
    {
        return writeBuffer;
    }
    
    /// <summary>
    /// Worker thread: Mark data as ready and swap buffers
    /// </summary>
    public void CommitData()
    {
        lock (bufferSwapLock)
        {
            // Swap buffers - write becomes read, read becomes write
            (writeBuffer, readBuffer) = (readBuffer, writeBuffer);
            dataReady = true;
            bufferSwapped = true;
        }
    }
    
    /// <summary>
    /// Main thread: Get the buffer to read agent states from
    /// </summary>
    public AgentStateArray GetReadBuffer()
    {
        lock (bufferSwapLock)
        {
            if (dataReady)
            {
                dataReady = false; // Mark as consumed
                return readBuffer;
            }
            return null; // No new data
        }
    }
    
    /// <summary>
    /// Reset the buffer system
    /// </summary>
    public void Clear()
    {
        lock (bufferSwapLock)
        {
            writeBuffer.Clear();
            readBuffer.Clear();
            dataReady = false;
            bufferSwapped = false;
        }
    }
}

/// <summary>
/// Array-based storage for agent states - Structure of Arrays for better cache performance
/// </summary>
public class AgentStateArray
{
    // Agent data arrays
    private Vector2[] positions;
    private Vector2[] forwards;
    private float[] speeds;
    private float[] turnAngles;
    private int[] agentIds;
    private bool[] isActive;
    private bool[] isOffTrack;
    
    // Metadata
    private int capacity;
    private int activeCount;
    
    public int Capacity => capacity;
    public int ActiveCount => activeCount;
    
    public AgentStateArray(int maxAgents)
    {
        capacity = maxAgents;
        positions = new Vector2[maxAgents];
        forwards = new Vector2[maxAgents];
        speeds = new float[maxAgents];
        turnAngles = new float[maxAgents];
        agentIds = new int[maxAgents];
        isActive = new bool[maxAgents];
        isOffTrack = new bool[maxAgents];
        activeCount = 0;
    }
    
    /// <summary>
    /// Add or update an agent's state in the buffer
    /// </summary>
    public void SetAgentState(int index, int agentId, Vector2 position, Vector2 forward, 
                             float speed, float turnAngle, bool active, bool offTrack)
    {
        if (index < 0 || index >= capacity) return;
        
        agentIds[index] = agentId;
        positions[index] = position;
        forwards[index] = forward;
        speeds[index] = speed;
        turnAngles[index] = turnAngle;
        isActive[index] = active;
        isOffTrack[index] = offTrack;
        
        // Update active count
        if (active && index >= activeCount)
        {
            activeCount = index + 1;
        }
    }
    
    /// <summary>
    /// Get agent state at specific index
    /// </summary>
    public bool GetAgentState(int index, out int agentId, out Vector2 position, out Vector2 forward,
                             out float speed, out float turnAngle, out bool active, out bool offTrack)
    {
        if (index < 0 || index >= capacity || index >= activeCount)
        {
            agentId = -1;
            position = Vector2.Zero;
            forward = Vector2.Zero;
            speed = 0f;
            turnAngle = 0f;
            active = false;
            offTrack = false;
            return false;
        }
        
        agentId = agentIds[index];
        position = positions[index];
        forward = forwards[index];
        speed = speeds[index];
        turnAngle = turnAngles[index];
        active = isActive[index];
        offTrack = isOffTrack[index];
        return true;
    }
    
    /// <summary>
    /// Mark an agent as inactive (for removal)
    /// </summary>
    public void DeactivateAgent(int index)
    {
        if (index >= 0 && index < capacity)
        {
            isActive[index] = false;
        }
    }
    
    /// <summary>
    /// Compact the array by removing inactive agents
    /// </summary>
    public void CompactArray()
    {
        int writeIndex = 0;
        
        for (int readIndex = 0; readIndex < activeCount; readIndex++)
        {
            if (isActive[readIndex])
            {
                if (writeIndex != readIndex)
                {
                    // Move active agent data down
                    agentIds[writeIndex] = agentIds[readIndex];
                    positions[writeIndex] = positions[readIndex];
                    forwards[writeIndex] = forwards[readIndex];
                    speeds[writeIndex] = speeds[readIndex];
                    turnAngles[writeIndex] = turnAngles[readIndex];
                    isActive[writeIndex] = isActive[readIndex];
                    isOffTrack[writeIndex] = isOffTrack[readIndex];
                }
                writeIndex++;
            }
        }
        
        activeCount = writeIndex;
    }
    
    /// <summary>
    /// Clear all agent data
    /// </summary>
    public void Clear()
    {
        activeCount = 0;
        // Note: We don't need to clear arrays, just reset the count
    }
    
    /// <summary>
    /// Set the active count (useful when batch updating)
    /// </summary>
    public void SetActiveCount(int count)
    {
        activeCount = Math.Clamp(count, 0, capacity);
    }
}