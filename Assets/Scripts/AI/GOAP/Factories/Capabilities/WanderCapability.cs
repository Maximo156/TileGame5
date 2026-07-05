using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class WanderCapability : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder(nameof(WanderCapability));

            builder.AddGoal<WanderGoal>()
                .SetBaseCost(50)
                .AddCondition<IsWandering>(Comparison.GreaterThanOrEqual, 1);

            builder.AddAction<WaitAtTargetAction>()
                .SetTarget<WanderTarget>()
                .AddEffect<IsWandering>(EffectType.Increase)
                .SetProperties(new WaitAtTargetAction.Props
                {
                    minTimer = 5f,
                    maxTimer = 10f
                })
                .AddCondition<FearKey>(Comparison.SmallerThan, Constants.Config.FearThreshold);

            builder.AddTargetSensor<WanderTargetSensor>()
                .SetCallback((sensor) =>
                {
                    sensor.Range = 5;
                })
                .SetTarget<WanderTarget>();

            return builder.Build();
        }
    }
}