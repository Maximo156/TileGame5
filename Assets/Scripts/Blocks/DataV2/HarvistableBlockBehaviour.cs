using NativeRealm;
using System.Collections.Generic;
using UnityEngine;

namespace ComposableBlocks
{
    public class HarvistableBlockBehaviour : BlockBehaviour, IInteractableBehaviour
    {
        public Block AfterHarvest;
        public bool useOverrides;
        public List<ItemStack> HarvestOverrides;

        public bool Interact(ref NativeBlockSlice slice, InteractionWorldInfo worldInfo, InteractorInfo interactor)
        {
            slice.wallBlock = AfterHarvest?.Id ?? 0;
            Utilities.DropItems(worldInfo.WorldPos, useOverrides ? HarvestOverrides : worldInfo.block.Drops);
            return true;
        }
    }
}
