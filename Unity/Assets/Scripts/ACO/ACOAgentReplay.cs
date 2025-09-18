using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class ACOAgentReplay : MonoBehaviour
{
    List<ReplayState> replayStates;
    int updateRate = 50;
    float remainingTime = 0;
    bool playing = false;
    public GameObject model;
    private int currentStep = 0;
    private LineRenderer rend;
    public void InitializeString(string data)
    {
        string temp = data;
        replayStates = new();

        while (temp.Length > 0)
        {
            int pos = temp.IndexOf('\n');
            string line = temp[..pos];
            replayStates.Add(ReplayState.Parse(line));
            temp = temp[(pos + 1)..];
        }

        DrawLine();
    }

    private void DrawLine()
    {
        rend.positionCount = replayStates.Count;
        int count = 0;
        replayStates.ForEach((state) => rend.SetPosition(count++, new Vector3(state.position.X, 1, state.position.Y)));
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
        rend = gameObject.AddComponent<LineRenderer>();
        rend.startWidth = 50.0f;
        rend.endWidth = 50.0f;
    }
    
    private void UpdateView()
    {
        remainingTime += 1.0f / updateRate;
        ReplayState state = replayStates[currentStep];
        model.transform.position = new Vector3(state.position.X, 1, state.position.Y);
        model.transform.rotation = Quaternion.Euler(0.0f, state.bear, 0.0f);
        currentStep++;
    }
}