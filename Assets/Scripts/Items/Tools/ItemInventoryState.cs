using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ItemInventoryState : ItemState, IGridSource
{
    LimitedInventory inv;
    public ItemInventoryState(Func<Item, bool> isAllowed, int slots)
    {
        inv = new LimitedInventory(isAllowed, slots);
    }

    public IEnumerable<IGridItem> GetGridItems() => inv.GetGridItems();
}
