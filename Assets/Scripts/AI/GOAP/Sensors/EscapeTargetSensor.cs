using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Goap
{
    public class EscapeTargetSensor : LocalTargetSensorBase
    {
        public float dist = 7;
        public float variance = 4;

        public override void Created()
        {}

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            var fear = references.GetCachedComponent<FearBehaviour>();

            if (fear.Attacker == null)
                return null;

            return new PositionTarget(PickTarget(agent.Transform.position, fear.Attacker.position));
        }

        public override void Update()
        {}

        private Vector2 PickTarget(Vector3 self, Vector3 attacker)
        {
            var away =
                (self - attacker).normalized;

            Vector2 random =
                Random.insideUnitCircle * variance;

            return self +
                    away * dist +
                    new Vector3(random.x, 0f, random.y);
        }
    }
}
