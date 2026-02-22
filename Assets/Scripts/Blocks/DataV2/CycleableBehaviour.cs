using NativeRealm;
using UnityEngine;

namespace ComposableBlocks
{
    public class CycleableBehaviour : BlockBehaviour, ISimpleStateBlockBehaviour, IInteractableBehaviour
    {
        public byte totalStates = 2;
        public byte GetState()
        {
            return 0;
        }

        public bool Interact(ref NativeBlockSlice slice, InteractionWorldInfo worldInfo, InteractorInfo interactor)
        {
            slice.simpleBlockState = (byte)((slice.simpleBlockState++) % totalStates);
            return true;
        }
    }
}
