using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class PanicCapability : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder(nameof(PanicCapability));

            builder.AddGoal<ReduceFearGoal>()
                .SetBaseCost(40)
                .AddCondition<FearKey>(Comparison.SmallerThan, Constants.Config.FearThreshold);

            builder.AddAction<RunAwayAction>()
                .SetTarget<EscapeTargetKey>()
                .AddEffect<FearKey>(EffectType.Decrease);

            builder.AddWorldSensor<FearSensor>()
                .SetKey<FearKey>();
            builder.AddTargetSensor<EscapeTargetSensor>()
                .SetTarget<EscapeTargetKey>();

            return builder.Build();
        }
    }
}
