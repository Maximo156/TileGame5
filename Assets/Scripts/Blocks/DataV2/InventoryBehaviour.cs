using Newtonsoft.Json;
using UnityEngine;

namespace ComposableBlocks
{
    public class InventoryBehaviour : BlockBehaviour, IStatefulBlockBehaviour, IInterfaceBlockBehaviour, ILootableBlockBehaviour
    {
        public int size;

        public BlockBehaviourState GetState(Block baseBlock)
        {
            return new InventoryBehaviourState(size);
        }
    }

    public class InventoryBehaviourState : BlockBehaviourState, IStorageBlockBehaviourState
    {
        public Inventory StoredItems;

        [JsonConstructor]
        public InventoryBehaviourState(Inventory StoredItems)
        {
            this.StoredItems = StoredItems;
            StoredItems.OnItemChanged += (_) => TriggerStateChange();
        }

        public InventoryBehaviourState(int count)
        {
            StoredItems = new Inventory(count);
            StoredItems.OnItemChanged += (_) => TriggerStateChange();
        }

        public override void CleanUp(Vector2Int pos)
        {
            Utilities.DropItems(pos, StoredItems.GetAllItems(false));
        }

        public bool AddItemStack(ItemStack stack)
        {
            return StoredItems.AddItem(stack);
        }
    }
}
