using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockTool", menuName = "Inventory/BlockTool", order = 1)]
public class BlockTool : Tool
{
    public Block block;

    BlockRecipe recipe = new();

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        var blockPos = Vector2Int.FloorToInt(targetPosition.ToVector2());
        ChunkManager.TryGetBlock(blockPos, out var blockSlice);

        if (CanReach(usePosition, targetPosition))
        {
            if(TryReplace(blockPos, block, blockSlice))
            {
                ChunkManager.PlaceBlock(blockPos, recipe.block);
            }
            else if(recipe is not null &&
                    recipe.CanProduce(useInfo.availableInventory.GetAllItems()))
            {
                if (ChunkManager.PlaceBlock(blockPos, recipe.block))
                {
                    recipe.UseRecipe(useInfo.availableInventory);
                }
            }
        }
    }

    bool TryReplace(Vector2Int worldPos, Block block, BlockSlice slice)
    {
        Block blockToWorkWith = block is Wall ? slice.WallBlock : (block is Ground ? slice.GroundBlock : (block is Roof ? slice.RoofBlock : null));
        if (blockToWorkWith == null) return false;
        if (recipe.CanProduce(blockToWorkWith.Drops))
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
