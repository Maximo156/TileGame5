using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class FearSensor : LocalWorldSensorBase
    {
        public override void Created()
        {}

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            return (int)references.GetCachedComponent<FearBehaviour>().Fear;
        }

        public override void Update()
        {}
    }
}
