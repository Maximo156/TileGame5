using System.Collections.Generic;
using UnityEngine;

namespace ComposableBlocks
{
    public class CrateBehaviour : BlockBehaviour, IStatefulBlockBehaviour
    {
        public BlockBehaviourState GetState(Block baseBlock)
        {
            return new CrateBehaviourState();
        }
    }

    public class CrateBehaviourState : BlockBehaviourState, IStorageBlockBehaviourState
    {
        public List<ItemStack> AdditionalDrops;

        public bool AddItemStack(ItemStack stack)
        {
            if (AdditionalDrops == null)
            {
                AdditionalDrops = new List<ItemStack>();
            }
            AdditionalDrops.Add(stack);
            return true;
        }

        public override void CleanUp(Vector2Int pos)
        {
            if (AdditionalDrops is not null)
            {
                Utilities.DropItems(pos, AdditionalDrops);
            }
        }
    }
}
