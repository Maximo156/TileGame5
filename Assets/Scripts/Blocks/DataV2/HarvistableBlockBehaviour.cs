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

        public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice, InteractorInfo interactor)
        {
            slice.wallBlock = AfterHarvest?.Id ?? 0;
            Utilities.DropItems(worldPos, useOverrides ? HarvestOverrides : throw new System.Exception("Fix harvest blocks"));
            return true;
        }
    }
}
