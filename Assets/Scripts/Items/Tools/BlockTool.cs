using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockTool", menuName = "Inventory/BlockTool", order = 1)]
public class BlockTool : Tool
{
    public Block block;

    BlockRecipe recipe = new();

    bool UseState => MaxStackSize == 1;

    public override bool PerformAction(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (CanReach(usePosition, targetPosition))
        {
            var blockPos = Vector2Int.FloorToInt(targetPosition.ToVector2());
            ChunkManager.TryGetBlock(blockPos, out var blockSlice);
            var rawDir = (targetPosition - usePosition).ToVector2();
            var xBig = Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.y);
            var dir = new Vector2Int(xBig ? (int)Mathf.Sign(rawDir.x) : 0, !xBig ? (int)Mathf.Sign(rawDir.y) : 0);
            if (TryReplace(blockPos, block, blockSlice))
            {
                ChunkManager.PlaceBlock(blockPos, dir, recipe.block);
                return UseState;
            }
            else if (recipe is not null &&
                    recipe.CanProduce(useInfo.availableInventory.GetAllItems()))
            {
                if (ChunkManager.PlaceBlock(blockPos, dir, recipe.block))
                {
                    recipe.UseRecipe(useInfo.availableInventory);
                    return UseState;
                }
            }
        }
        return false;
    }

    public override ItemState GetItemState()
    {
        if(!UseState)
        {
            return null;
        }
        return base.GetItemState();
    }

    bool TryReplace(Vector2Int worldPos, Block block, BlockSlice slice)
    {
        Block blockToWorkWith = block is Wall ? slice.WallBlock : (block is Ground ? slice.GroundBlock : (block is Roof ? slice.RoofBlock : null));
        if (blockToWorkWith == null || (block is Ground && slice.WallBlock is not null)) return false;
        if (recipe.CanProduce(blockToWorkWith.Drops) && block is not IConditionalPlace)
        {
            slice.Break(worldPos, block is Roof, out var _, true);
            Utilities.DropItems(worldPos, recipe.UseRecipe(blockToWorkWith.Drops));
            return true;
        }
        return false;
    }

    private void OnValidate()
    {
        recipe.block = block;
    }
}
