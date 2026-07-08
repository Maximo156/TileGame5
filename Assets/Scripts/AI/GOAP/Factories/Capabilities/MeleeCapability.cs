using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class MeleeCapability : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder(nameof(MeleeCapability));

            builder.AddGoal<KillEnemyGoal>()
                .SetBaseCost(0)
                .AddCondition<EnemyHealth>(Comparison.SmallerThanOrEqual, 0);

            builder.AddAction<MeleeAction>()
                .SetTarget<ClosestEnemyKey>()
                .AddEffect<EnemyHealth>(EffectType.Decrease)
                .SetStoppingDistance(0.5f)
                .SetValidateTarget(true);

            builder.AddTargetSensor<EnemyTargetSensor>()
                .SetTarget<ClosestEnemyKey>();

            return builder.Build();
        }
    }
}