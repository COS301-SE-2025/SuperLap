using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class ACOAgentReplay : MonoBehaviour
{
    struct ReplayState
    {
        public System.Numerics.Vector2 position;
        public System.Numerics.Vector2 forward;
    }
    List<ReplayState> replayStates;
    List<(int, int)> inputs;
    ACOAgent modelAgent;
    int updateRate = 50;
    float remainingTime = 0;
    bool playing = false;
    public GameObject model;
    private int currentStep = 0;
    private LineRenderer renderer;
    public void InitializeString(string data)
    {
        inputs = new();
        string temp = data;

        int p = temp.IndexOf('\n');
        string l = temp.Substring(0, p);
        string[] s = l.Split(':');
        float posX = float.Parse(s[0]);
        float posY = float.Parse(s[1]);
        float bear = float.Parse(s[2]);
        temp = temp.Substring(p + 1);

        while (temp.Length > 0)
        {
            int pos = temp.IndexOf('\n');
            string line = temp.Substring(0, pos);
            string[] split = line.Split(':');
            inputs.Add((int.Parse(split[0]), int.Parse(split[1])));
            temp = temp.Substring(pos + 1);
        }

        SimulatePath(new System.Numerics.Vector2(posX, posY), bear);
    }

    private void SimulatePath(System.Numerics.Vector2 pos, float bear)
    {
        modelAgent = new ACOAgent(null, pos, bear);
        replayStates = new();

        // Set the LineRenderer position count to match the number of inputs
        renderer.positionCount = inputs.Count;

        // Iterate through inputs without modifying the list
        for (int i = 0; i < inputs.Count; i++)
        {
            (int throttle, int steer) = inputs[i];

            modelAgent.SetInput(throttle, steer);
            modelAgent.Step();

            replayStates.Add(new ReplayState
            {
                position = modelAgent.Position,
                forward = modelAgent.Forward
            });

            // Set the LineRenderer position for this step
            renderer.SetPosition(i, new Vector3(modelAgent.Position.X, 1, modelAgent.Position.Y));
        }

        Debug.Log("Path simulated!");
    }

    public void InitializeTextFile(string fileName)
    {
        string data = "";
        using (StreamReader reader = new StreamReader(fileName))
        {
            data = reader.ReadToEnd();
        }

        InitializeString(data);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playing = !playing;
        }

        if (playing)
        {
            if (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
            }
            else
            {
                UpdateView();
            }
        }
    }

    public void Start()
    {
        renderer = gameObject.AddComponent<LineRenderer>();
    }
    
    private void UpdateView()
    {
        remainingTime += 1.0f / updateRate;
        ReplayState state = replayStates[currentStep];
        model.transform.position = new Vector3(state.position.X, 1, state.position.Y);
        model.transform.forward = new Vector3(state.forward.X, 0, state.forward.Y);
        currentStep++;
    }
}