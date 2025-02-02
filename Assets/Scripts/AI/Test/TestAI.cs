using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStarSharp;

public class TestAI : MonoBehaviour, IAI, IBehavior
{
    public Navigator navigator;
    public bool useManager;

    public Transform Transform => transform;

    public IPathFinder pathfinder => null;

    public IBehavior behavior => this;

    public bool Natural => false;

    private void Start()
    {
        AIManager.Register(this);
        navigator.onFailedPathing = (PathFindingResult _) => print(name +" stuck");
    }

    public void Update()
    {
        navigator.Move(Time.deltaTime);
    }

    public Vector2Int Step(float deltaTime)
    {
        if(navigator.state == Navigator.State.Idle)
        {
            SetRandomGoal(null);
        }
        return Vector2Int.zero;
    }

    public void SetRandomGoal(PathFindingResult _)
    {
        navigator.Goal = Utilities.RandomVector2Int(30);
    }
}
