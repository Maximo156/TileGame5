using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Test : MonoBehaviour, IAI, IBehavior
{
    public ManualUpdateStateMachine stateMachine;

    public IPathFinder pathfinder => null;

    public IBehavior behavior => this;

    public Transform Transform => transform;

    public IAI ai => this;

    private void Start()
    {
        ai.Register();
    }

    public Vector2Int Step(float deltaTime)
    {
        stateMachine.Step(deltaTime);
        StateMachine machine;
        return default;
    }
}
