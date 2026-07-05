using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Goap
{
    [GoapId("RunAway-3bd5c586-d133-4117-96de-34284c7a1bd3")]
    public class RunAwayAction : GoapActionBase<RunAwayAction.Data>
    {
        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            return ActionRunState.Completed;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}