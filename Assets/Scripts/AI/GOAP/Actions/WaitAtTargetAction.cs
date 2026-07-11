
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using System;
using Random = UnityEngine.Random;
namespace Goap
{
    public class WaitAtTargetAction : GoapActionBase<WaitAtTargetAction.Data, WaitAtTargetAction.Props>
    {
        public override void Start(IMonoAgent agent, Data data)
        {
            var wait = Random.Range(Properties.minTimer, Properties.maxTimer);

            data.Timer = ActionRunState.Wait(wait);
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            if (data.Timer.IsRunning())
                return data.Timer;

            return ActionRunState.Completed;
        }

        [Serializable]
        public class Props : IActionProperties
        {
            public float minTimer;
            public float maxTimer;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public IActionRunState Timer { get; set; }
        }
    }
}