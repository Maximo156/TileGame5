using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using EntityStatistics;
using Goap;
using System;
using UnityEngine;

public class AggressiveMobBrain : BaseMobBrain
{
    public LayerMask EnemyMask;
    EntityStats stats;
    EnemySensor sensor;

    protected override void Awake()
    {
        base.Awake();
        stats = GetComponent<EntityStats>();

        sensor = GetComponentInChildren<EnemySensor>();
        sensor.Init(stats);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        sensor.OnEnemyEnter += OnEnemySeen;
        provider.Events.OnNoActionFound += OnNoActionFound;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        sensor.OnEnemyEnter -= OnEnemySeen;
        provider.Events.OnNoActionFound -= OnNoActionFound;
    }

    protected override void InitAgentType()
    {
        if (provider.AgentTypeBehaviour == null)
            provider.AgentType = goap.GetAgentType(nameof(AggressiveMobTypeConfigFactory));
    }

    private void Start()
    {
        SetBaseGoals();
    }

    void SetBaseGoals()
    {
        provider.RequestGoal<WanderGoal>();
    }

    public void Attack()
    {
        var target = agent.CurrentTarget;
        if (target is not TransformTarget otherTransform) return;

        var ingress = otherTransform.Transform.GetComponentInParent<HitIngress>();
        if (ingress != null) 
        { 
            ingress.Hit(new HitData() { Perpetrator = transform, Damage = stats.GetStat(EntityStats.Stat.BaseDamage) });
        }
    }

    private void OnEnemySeen()
    {
        agent.StopAction();
        provider.RequestGoal<KillEnemyGoal>();
    }

    private void OnNoActionFound(IGoalRequest goal)
    {
        SetBaseGoals();
    }
}
