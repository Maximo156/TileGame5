using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHarvistableBlock", menuName = "Block/HarvistableBlock", order = 1)]
public class HarvistableBlock : Wall, IInteractableBlock
{
    public Block AfterHarvest;
    public bool Interact(Vector2Int worldPos, BlockSlice slice)
    {
        if (AfterHarvest != null)
        {
            slice.SetBlock(AfterHarvest);
            Utilities.DropItems(worldPos, Drops);
        }
        else
        {
            slice.Break(worldPos, false, out var _);
        }
        return true;
    }
}
