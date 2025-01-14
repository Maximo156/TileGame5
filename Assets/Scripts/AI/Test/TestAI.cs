using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStarSharp;

public class TestAI : MonoBehaviour, IStepable
{
    public Navigator navigator;
    public bool useManager;

    private void Start()
    {
        if (useManager)
        {
            AIManager.Register(this);
        }
        navigator.onFailedPathing = (PathFindingResult _) => print(name +" stuck");
    }

    public void Update()
    {
        if (!useManager)
        {
            Step(Time.deltaTime);
        }
    }

    public Vector2Int Step(float deltaTime)
    {
        navigator.Move(Time.deltaTime);
        if(navigator.state == Navigator.State.Idle)
        {
            SetRandomGoal(null);
        }
        return Vector2Int.zero;
    }

    public void SetRandomGoal(PathFindingResult _)
    {
        navigator.Goal = Utilities.RandomVector2Int(100);
    }
}
