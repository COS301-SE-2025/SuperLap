using System.Collections.Generic;
using UnityEngine;

public class Trainer : MonoBehaviour
{
    [SerializeField] private int agentCount = 0;
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int decisionInterval = 1;
    private List<List<(int, int)>> inputs;
    private List<MotorcycleAgent> agents;
    private bool isTraining = false;
    private int currentStep = 0;
    Vector3 startPos;
    Vector3 startDir;

    void Start()
    {

    }

    void Clear()
    {
        inputs.Clear();
        foreach (MotorcycleAgent agent in agents)
        {
            Destroy(agent.gameObject);
        }
        agents.Clear();
    }

    void Initialize()
    {
        startPos = TrackMaster.GetTrainingSpawnPosition(0, TrackMaster.GetCurrentRaceline());
        startDir = TrackMaster.GetTrainingSpawnDirection(0, TrackMaster.GetCurrentRaceline());
        inputs = new List<List<(int, int)>>();
        agents = new List<MotorcycleAgent>();
        for (int i = 0; i < agentCount; i++)
        {
            GameObject obj = Instantiate(agentPrefab);
            obj.transform.position = startPos;
            obj.transform.rotation = Quaternion.LookRotation(startDir);
            MotorcycleAgent agent = obj.GetComponent<MotorcycleAgent>();
            agents.Add(agent);
            inputs.Add(new List<(int, int)>());
        }
        decisionInterval = Mathf.Max(agentCount, decisionInterval);
        currentStep = 0;
    }

    void FixedUpdate()
    {
        if (isTraining)
        {
            foreach (MotorcycleAgent agent in agents)
            {
                int idx = agents.IndexOf(agent);
                agent.Step();

                if((currentStep+idx)% decisionInterval != 0)
                {
                    continue;
                }
                (int, int) action = agent.Decide();
                agent.SetInput(action.Item1, action.Item2);
                inputs[idx].Add(action);
            }

            currentStep++;
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            Clear();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Initialize();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            isTraining = !isTraining;
            if (isTraining)
            {
                Debug.Log("Training started");
            }
            else
            {
                Debug.Log("Training stopped");
            }
        }
        if(Input.GetKeyDown(KeyCode.S))
        {
            foreach (MotorcycleAgent agent in agents)
            {
                agent.Step();
            }
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            foreach (MotorcycleAgent agent in agents)
            {
                agent.SetInput(1, 0);
            } 
        }
    }
}
