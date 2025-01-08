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
    LimitedInventory inv;
    public ItemInventoryState(Func<Item, bool> isAllowed, int slots)
    {
        inv = new LimitedInventory(isAllowed, slots);
    }

    public ItemInventoryState Inventory => this;

    public IEnumerable<IGridItem> GetGridItems() => inv.GetGridItems();
}
