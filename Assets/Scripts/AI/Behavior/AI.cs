using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class AI : MonoBehaviour, IBehavior, IAI
{
    public JobNavigator navigator;

    public Transform Transform => transform;

    public IPathFinder pathfinder => navigator;

    public IBehavior behavior => this;

    private void Start()
    {
        AIManager.Register(this);
    }

    public void Update()
    {
        navigator.Move(Time.deltaTime);
    }

    public Vector2Int Step(float deltaTime)
    {
        if (navigator.state == JobNavigator.State.Idle)
        {
            SetRandomGoal();
        }
        return Vector2Int.zero;
    }

    public void SetRandomGoal()
    {
        var g = Utilities.RandomVector2Int(30);
        navigator.Goal = new int2(g.x, g.y);
    }
}
