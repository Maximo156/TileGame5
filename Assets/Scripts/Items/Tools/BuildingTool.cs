using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingTool", menuName = "Inventory/BuildingTool", order = 1)]
public class BuildingTool : Tool, IGridSource
{
    [Header("Building Categories")]
    public List<Category> BuildingCategories;

    public override bool PerformAction(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        var recipe = (useInfo.state as BuildingToolState)?.selectedRecipe;
        if (CanReach(usePosition, targetPosition) &&
            recipe is not null &&
            recipe.CanProduce(useInfo.availableInventory.GetAllItems()))
        {
            if (ChunkManager.PlaceBlock(Vector2Int.FloorToInt(targetPosition.ToVector2()), recipe.block))
            {
                recipe.UseRecipe(useInfo.availableInventory);
                return true;
            }
        }
        return false;
    }

    public IEnumerable<IGridItem> GetGridItems()
    {
        return BuildingCategories.Count == 1 ? BuildingCategories[0].recipes : BuildingCategories;
    }

    public override ItemState GetItemState()
    {
        return new BuildingToolState(this);
    }
}

public class BuildingToolState : ItemState, IDurableState, IGridClickListener
{
    public BlockRecipe selectedRecipe;

    public DurableState Durability { get; }

    public BuildingToolState(BuildingTool tool)
    {
        Durability = new DurableState(tool.MaxDurability, this);
    }

    public void OnClick(IGridItem item)
    {
        if (item is BlockRecipe recipe)
        {
            selectedRecipe = recipe;
        }
    }

    public override string GetStateString()
    {
        return selectedRecipe is not null ? "Building: " +selectedRecipe.block.name.Replace("Block", "").Replace("Item", "").SplitCamelCase() : "";
    }
}
