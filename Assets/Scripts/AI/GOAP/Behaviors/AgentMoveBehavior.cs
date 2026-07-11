
using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent (typeof(AgentBehaviour))]
public class AgentMoveBehavior : MonoBehaviour
{
    private GridNavAgent navAgent;
    private AgentBehaviour agentBehaviour;

    private void Awake()
    {
        agentBehaviour = GetComponent<AgentBehaviour>();
        navAgent = GetComponent<GridNavAgent>();
    }

    private void OnEnable()
    {
        agentBehaviour.Events.OnTargetChanged += OnTargetChanged;
        agentBehaviour.Events.OnTargetInRange += OnTargetInRange;
        agentBehaviour.Events.OnTargetNotInRange += OnTargetNotInRange;
        agentBehaviour.Events.OnPause += navAgent.Pause;
        agentBehaviour.Events.OnResume += navAgent.Resume;
    }

    private void OnDisable()
    {
        agentBehaviour.Events.OnTargetChanged -= OnTargetChanged;
        agentBehaviour.Events.OnPause -= navAgent.Pause;
        agentBehaviour.Events.OnResume -= navAgent.Resume;
        agentBehaviour.Events.OnPause -= navAgent.Pause;
        agentBehaviour.Events.OnResume -= navAgent.Resume;
    }

    private void OnTargetChanged(ITarget target, bool inRange)
    {
        navAgent.SetTarget(target);
    }

    private void OnTargetNotInRange(ITarget target)
    {
        navAgent.Resume();
    }

    private void OnTargetInRange(ITarget target)
    {
        navAgent.Pause();
    }

}