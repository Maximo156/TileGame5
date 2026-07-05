using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class FollowHeldItemCapability : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            var builder = new CapabilityBuilder(nameof(FollowHeldItemCapability));

            builder.AddGoal<FollowHeldItemGoal>()
                .SetBaseCost(40)
                .AddCondition<IsFollowingHeldItem>(Comparison.GreaterThanOrEqual, 1);

            builder.AddAction<WaitAtTargetAction>()
                .SetTarget<ClosestPlayerInventory>()
                .AddEffect<IsFollowingHeldItem>(EffectType.Increase)
                .SetProperties(new WaitAtTargetAction.Props
                {
                    minTimer = 5f,
                    maxTimer = 10f
                }).AddCondition<IsItemHeld>(Comparison.GreaterThanOrEqual, Constants.Config.FearThreshold)
                .AddCondition<FearKey>(Comparison.SmallerThan, 10);

            builder.AddMultiSensor<HeldItemSensor>();

            return builder.Build();
        }
    }
}