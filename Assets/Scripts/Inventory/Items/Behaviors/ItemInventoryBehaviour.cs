using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemInventoryBehaviour : ItemBehaviour, IStatefulItemBehaviour
{
    public abstract ItemBehaviourState GetNewState();
}

public class ItemInventoryBehaviourState : ItemBehaviourState, IGridSource
{
    public LimitedInventory inv { get; }
    public ItemInventoryBehaviourState(Func<Item, ItemStack[], int, bool> isAllowed, int slots)
    {
        inv = new LimitedInventory(isAllowed, slots);
    }

    public virtual IEnumerable<IGridItem> GetGridItems() => inv.GetGridItems();
}
