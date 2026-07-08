using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using EntityStatistics;
using System.Linq;
using UnityEngine;

namespace Goap
{
    public class EnemyTargetSensor : LocalTargetSensorBase
    {
        static LayerMask TerrainLayerMask = LayerMask.GetMask("Terrain");
        public override void Created()
        {}

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            var viewRange = references.GetCachedComponent<EntityStats>().GetStat(EntityStats.Stat.ViewDistance);
            var mask = references.GetCachedComponentInChildren<EnemySensor>().enemyLayer;

            if (existingTarget != null && existingTarget.IsValid())
            {
                return existingTarget;
            }

            var colliders = Physics2D.OverlapCircleAll(agent.Transform.position, viewRange, mask);

            if (colliders.Length == 0) return null;

            foreach ( var collider in colliders.OrderBy(c => Vector3.Distance(agent.Transform.position, c.transform.position)))
            {
                if(!Physics2D.Raycast(agent.Transform.position, collider.transform.position - agent.Transform.position, viewRange, TerrainLayerMask))
                {
                    return new VisibleTransformTarget(collider.transform, agent.Transform, viewRange);
                }
            }

            return null;
        }

        public override void Update()
        { }
    }
}
