using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

[AddComponentMenu("Visual Scripting/Manual State Machine")]
[RequireComponent(typeof(Variables))]
[DisableAnnotation]
public class ManualUpdateStateMachine : EventMachine<StateGraph, StateGraphAsset>, IBehavior
{
    protected override void OnEnable()
    {
        if (hasGraph)
        {
            using (var flow = Flow.New(reference))
            {
                graph.Start(flow);
            }
        }

        base.OnEnable();
    }

    protected override void OnInstantiateWhileEnabled()
    {
        if (hasGraph)
        {
            using (var flow = Flow.New(reference))
            {
                graph.Start(flow);
            }
        }

        base.OnInstantiateWhileEnabled();
    }

    protected override void OnUninstantiateWhileEnabled()
    {
        base.OnUninstantiateWhileEnabled();

        if (hasGraph)
        {
            using (var flow = Flow.New(reference))
            {
                graph.Stop(flow);
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (hasGraph)
        {
            using (var flow = Flow.New(reference))
            {
                graph.Stop(flow);
            }
        }
    }

    [ContextMenu("Show Data...")]
    protected override void ShowData()
    {
        base.ShowData();
    }

    public override StateGraph DefaultGraph()
    {
        return StateGraph.WithStart();
    }

    protected override void Update()
    {}

    protected override void FixedUpdate()
    {}

    protected override void LateUpdate()
    {}

    public Vector2Int Step(float deltaTime)
    {
        base.Update();
        base.FixedUpdate();
        base.LateUpdate();
        return default;
    }
}
