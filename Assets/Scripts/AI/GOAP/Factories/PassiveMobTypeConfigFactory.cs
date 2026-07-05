using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class PassiveMobTypeConfigFactory : AgentTypeFactoryBase
    {
        public override IAgentTypeConfig Create()
        {
            var builder = this.CreateBuilder(nameof(PassiveMobTypeConfigFactory));

            builder.AddCapability<WanderCapability>();
            builder.AddCapability<FollowHeldItemCapability>();
            builder.AddCapability<PanicCapability>();

            return builder.Build();
        }
    }
}
