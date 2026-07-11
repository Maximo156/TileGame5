using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using EntityStatistics;
using System;
using UnityEngine;

namespace Goap
{
    [GoapId("Melee-6e4d7cbc-00fe-4b53-bf3c-c2c997aeab20")]
    public class MeleeAction : GoapActionBase<MeleeAction.Data, MeleeAction.Props>
    {
        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if(data.state == Data.State.NotStarted && Vector3.Distance(agent.transform.position, data.Target.Position) <= data.stats.GetStat(EntityStats.Stat.AttackRange))
            {
                data.state = Data.State.Animating;
                data.brain.PlayAnimation("Attack", () => data.state = Data.State.Complete, false);
            }
            if(data.state == Data.State.Complete)
            {
                return ActionRunState.Completed;
            }
            return ActionRunState.Continue;
        }

        public class Data : IActionData
        {
            public enum State
            {
                NotStarted,
                Animating,
                Complete
            }

            public ITarget Target { get; set; }

            [GetComponent]
            public BaseMobBrain brain { get; set; }

            [GetComponent]
            public EntityStats stats { get; set; }

            public State state { get; set; } = State.NotStarted;
        }

        [Serializable]
        public class Props : IActionProperties
        {
            public float AttackRange;
        }

    }
}