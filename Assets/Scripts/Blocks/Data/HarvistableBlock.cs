using NativeRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHarvistableBlock", menuName = "Block/HarvistableBlock", order = 1)]
public class HarvistableBlock : Wall, IInteractableBlock
{
    [Header("Harvest Info")]
    public Wall AfterHarvest;
    public bool useOverrides;
    public List<ItemStack> HarvestOverrides;

    public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice, InteractorInfo interactor)
    {
        slice.wallBlock = AfterHarvest?.Id ?? 0;
        Utilities.DropItems(worldPos, useOverrides ? HarvestOverrides : Drops);
        return true;
    }
}
