using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftingBlock", menuName = "Block/CraftingBlock", order = 1)]
public class CraftingBlock : Wall, IGridSource, IInterfaceBlock
{
    public List<ItemRecipe> Recipes;

    public IEnumerable<IGridItem> GetGridItems() => Recipes;
}
