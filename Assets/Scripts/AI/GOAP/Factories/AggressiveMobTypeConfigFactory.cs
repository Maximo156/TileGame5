using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class AggressiveMobTypeConfigFactory : AgentTypeFactoryBase
    {
        public override IAgentTypeConfig Create()
        {
            var builder = CreateBuilder(nameof(AggressiveMobTypeConfigFactory));

            builder.AddCapability<WanderCapability>();
            builder.AddCapability<MeleeCapability>();

            builder.CreateCapability("NoFearCapability", capability =>
            {
                capability.AddWorldSensor<ConstSensor>().SetKey<FearKey>();
            });

            return builder.Build();
        }
    }
}
