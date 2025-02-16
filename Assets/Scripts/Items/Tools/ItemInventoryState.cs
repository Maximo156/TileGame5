using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IItemInventoryState
{
    ItemInventoryState Inventory { get; }
}

public class ItemInventoryState : ItemState, IGridSource, IItemInventoryState
{
    public LimitedInventory inv { get; }
    public ItemInventoryState(Func<Item, ItemStack[], int, bool> isAllowed, int slots)
    {
        inv = new LimitedInventory(isAllowed, slots);
    }

    public ItemInventoryState Inventory => this;

    public IEnumerable<IGridItem> GetGridItems() => inv.GetGridItems();
}
