using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class AI : MonoBehaviour, IStepable, IPathFinder
{
    public JobNavigator navigator;

    public Transform Transform => transform;

    public int2 Position
    {
        get
        {
            var block = Utilities.GetBlockPos(transform.position);
            return new int2(block.x, block.y);
        }
    }
    public int2 Goal { get; set; }
    public bool NeedPath => navigator.NeedPath || navigator.state == JobNavigator.State.Idle;

    public bool CanUseDoor => navigator.CanUseDoors;

    private void Start()
    {
        AIManager.Register(this);
    }

    public bool SetPath(NativeStack<int2> stack) => navigator.SetPath(stack);

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
        Goal = new int2(g.x, g.y);
    }
}
