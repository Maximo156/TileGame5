using System;
using UnityEngine;

[Serializable]
public class PlaceBlockBehaviour : RangedUseBehavior
{
    public Block block;

    protected override (bool used, bool useDurability) UseRanged(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        var recipe = new BlockRecipe() { block = block };
        var blockPos = Vector2Int.FloorToInt(targetPosition.ToVector2());
        ChunkManager.TryGetBlock(blockPos, out var blockSlice);
        var rawDir = (targetPosition - usePosition).ToVector2();
        var xBig = Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.y);
        var dir = new Vector2Int(xBig ? (int)Mathf.Sign(rawDir.x) : 0, !xBig ? (int)Mathf.Sign(rawDir.y) : 0);
        useInfo.stack.GetState<DurabilityState>(out var durability);
        if (TryReplace(blockPos, block, blockSlice, recipe))
        {
            ChunkManager.PlaceBlock(blockPos, dir, recipe.block);
            return (true, true);
        }
        else if (recipe is not null &&
                recipe.CanProduce(useInfo.availableInventory.GetAllItems()))
        {
            if (ChunkManager.PlaceBlock(blockPos, dir, recipe.block))
            {
                recipe.UseRecipe(useInfo.availableInventory);
                return (true, true);
            }
        }
        return (false, false);
    }

    bool TryReplace(Vector2Int worldPos, Block block, BlockSlice slice, BlockRecipe recipe)
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
}
