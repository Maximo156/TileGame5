using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class GoapConfigInitializer : GoapConfigInitializerBase
    {
        public override void InitConfig(IGoapConfig config)
        {
            config.GoapInjector = this.GetComponent<GoapInjector>();
        }
    }
}