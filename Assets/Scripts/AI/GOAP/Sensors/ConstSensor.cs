using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Goap
{
    public class ConstSensor : GlobalWorldSensorBase
    {
        public int value = 0;
        public override void Created()
        {}

        public override SenseValue Sense()
        {
            return value;
        }
    }
}
