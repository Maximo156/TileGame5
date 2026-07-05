using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using Goap;
using System;

public class AggressiveMobBrain : BaseMobBrain
{
    CombatConfig combatConfig;
    EnemySensor sensor;

    protected override void Awake()
    {
        base.Awake();
        combatConfig = GetComponent<CombatBehavior>().CombatConfig;

        sensor = GetComponentInChildren<EnemySensor>();
        sensor.Init(combatConfig);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        sensor.OnEnemyEnter += OnEnemySeen;
        sensor.OnEnemyExit += OnEnemyLost;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        sensor.OnEnemyEnter -= OnEnemySeen;
        sensor.OnEnemyExit -= OnEnemyLost;
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
            ingress.Hit(new HitData() { Perpetrator = transform, Damage = combatConfig.BaseDamage });
        }
    }

    private void OnEnemyLost()
    {
        SetBaseGoals();
    }

    private void OnEnemySeen()
    {
        agent.StopAction();
        provider.RequestGoal<KillEnemyGoal>();
    }
}
