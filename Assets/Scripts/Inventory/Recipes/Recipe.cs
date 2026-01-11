using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Recipe: IGridItem, IGridSource
{
    public abstract List<ItemStack> Required { get; }

    public bool CanProduce(IEnumerable<ItemStack> items, bool skipRecipePossible = false)
    {
        if (skipRecipePossible && WorldSettings.UseRecipeInputs) return true;
        var dict = Utilities.ConvertToItemCounts(items);
        foreach (var itemstack in Required)
        {
            if (!dict.TryGetValue(itemstack.Item, out var count) || count < itemstack.Count)
            {
                return false;
            }
        }
        return true;
    }

    public void UseRecipe(IInventoryContainer inventory, bool skipRecipePossible = false)
    {
        if (skipRecipePossible && WorldSettings.UseRecipeInputs) return;
        foreach(var item in Required)
        {
            inventory.RemoveItemSafe(item);
        }
    }

    public IEnumerable<ItemStack> UseRecipe(IEnumerable<ItemStack> items)
    {
        var dict = Utilities.ConvertToItemCounts(items);
        foreach (var item in Required)
        {
            dict[item.Item] -= item.Count;
        }
        return dict.Where(kvp => kvp.Value > 0).Select(kvp => new ItemStack(kvp.Key, kvp.Value));
    }

    public abstract Sprite GetSprite();
    public abstract string GetString();
    public abstract Color GetColor();
    public abstract (string, string) GetTooltipString();

    public IEnumerable<IGridItem> GetGridItems()
    {
        return Required;
    }
}
