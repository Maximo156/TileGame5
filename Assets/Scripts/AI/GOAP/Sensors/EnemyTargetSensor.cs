using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Goap
{
    public class EnemyTargetSensor : LocalTargetSensorBase
    {
        public override void Created()
        {}

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            var config = references.GetCachedComponent<CombatBehavior>().CombatConfig;

            if (existingTarget != null && Vector3.Distance(agent.Transform.position, existingTarget.Position) < config.viewRange)
            {
                return existingTarget;
            }

            var colliders = Physics2D.OverlapCircleAll(agent.Transform.position, config.viewRange, config.mask);

            if (colliders.Length == 0) return null;

            return new TransformTarget(colliders.MinBy(c => Vector3.Distance(agent.Transform.position, c.transform.position)).transform);
        }

        public override void Update()
        { }
    }
}
