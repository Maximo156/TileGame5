using NativeRealm;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSliceState
{
    public Stack<ItemStack> placedItems;

    public BlockState blockState;

    public BlockSliceState()
    {

    }

    public bool PlaceItem(ItemStack item, NativeBlockSlice slice)
    {
        if(item is null)
        {
            return false;
        }
        if(placedItems is null)
        {
            placedItems = new();
        }
        if (slice.wallBlock == 0 && placedItems.Count == 0)
        {
            placedItems.Push(item);
            return true;
        }
        if (placedItems.TryPeek(out var placedItem) && placedItem.Item == item.Item)
        {
            placedItems.Peek().Combine(item);
            return item.Count == 0;
        }

        return false;
    }

    public ItemStack PopItem()
    {
        return placedItems?.Count == 0 ? null : placedItems?.Pop();
    }

    public void DropItems(Vector2Int worldPos)
    {
        if (placedItems is null) return;
        Utilities.DropItems(worldPos, placedItems);
        placedItems = null;
    }
}
