using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBehaviour : RangedUseBehavior, IGridSource, IStatefulItemBehaviour
{
    public List<Category> BuildingCategories;
    protected override (bool used, bool useDurability) UseRanged(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (useInfo.stack.GetState<BuildingBehaviourState>(out var state) &&
            state.selectedRecipe is not null &&
            state.selectedRecipe.CanProduce(useInfo.availableInventory.GetAllItems(), true))
        {
            var rawDir = (targetPosition - usePosition).ToVector2();
            var xBig = Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.y);
            var dir = new Vector2Int(xBig ? (int)Mathf.Sign(rawDir.x) : 0, !xBig ? (int)Mathf.Sign(rawDir.y) : 0);
            if (ChunkManager.PlaceBlock(Vector2Int.FloorToInt(targetPosition.ToVector2()), dir, state.selectedRecipe.block))
            {
                state.selectedRecipe.UseRecipe(useInfo.availableInventory, true);
                return (true, true);
            }
        }
        return (false, false);
    }

    public IEnumerable<IGridItem> GetGridItems()
    {
        return BuildingCategories.Count == 1 ? BuildingCategories[0].recipes : BuildingCategories;
    }

    public ItemBehaviourState GetNewState()
    {
        return new BuildingBehaviourState();
    }
}

public class BuildingBehaviourState : ItemBehaviourState, IGridClickListener, IStateStringProvider
{
    public BlockRecipe selectedRecipe;

    public void OnClick(IGridItem item)
    {
        if (item is BlockRecipe recipe)
        {
            selectedRecipe = recipe;
        }
    }

    public string GetStateString(Item _)
    {
        return selectedRecipe is not null ? "Building: " + selectedRecipe.block.name.Replace("Block", "").Replace("Item", "").SplitCamelCase() : "";
    }
}
