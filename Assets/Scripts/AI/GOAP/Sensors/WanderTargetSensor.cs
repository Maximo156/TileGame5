
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Goap
{
    public class WanderTargetSensor : LocalTargetSensorBase
    {
        public int Range;
        public override void Created() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            return new PositionTarget(Random.insideUnitCircle.ToVector3() * Range + agent.Transform.position);
        }

        public override void Update() { }
    }
}