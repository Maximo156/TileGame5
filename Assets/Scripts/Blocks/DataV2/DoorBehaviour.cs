using NativeRealm;
using UnityEngine;

namespace ComposableBlocks
{
    public class DoorBehaviour : CycleableBehaviour
    {
        public bool IsOpen(NativeBlockSlice slice)
        {
            return slice.simpleBlockState != 0;
        }
    }
}
