using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using Goap;
using System;
using UnityEngine;

public abstract class BaseMobBrain : MonoBehaviour
{
    protected AgentBehaviour agent;
    protected GoapActionProvider provider;
    protected GoapBehaviour goap;
    private MobAnimator animator;
    private GridNavAgent navAgent;

    protected virtual void Awake()
    {
        goap = FindAnyObjectByType<GoapBehaviour>();
        agent = GetComponent<AgentBehaviour>();
        provider = GetComponent<GoapActionProvider>();
        animator = GetComponent<MobAnimator>();
        navAgent = GetComponent<GridNavAgent>();

        InitAgentType();
    }

    protected virtual void OnEnable()
    {
        navAgent.OnLocomotionUpdate += animator.UpdateLocomotion;
        navAgent.OnStuck += OnStuck;
    }

    protected virtual void OnDisable()
    {
        navAgent.OnLocomotionUpdate -= animator.UpdateLocomotion;
        navAgent.OnStuck -= OnStuck;
    }

    private void OnStuck()
    {
        agent.StopAction();
    }

    protected abstract void InitAgentType();

    public void PlayAnimation(string animation, Action callback, bool pauseAgent = true)
    {
        if (pauseAgent)
        {
            agent.IsPaused = true;
        }
        animator.PlayAnimation(animation, callback);
    }

    public void ResumeAgent()
    {
        agent.IsPaused = false;
    }
}
