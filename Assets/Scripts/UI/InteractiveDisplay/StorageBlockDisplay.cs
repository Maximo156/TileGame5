using ComposableBlocks;
using NativeRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageBlockDisplay : InteractiveDislay
{
    public SingleInventoryDisplay singleInventoryDisplay;
    private Inventory _attachedInv;
    public override Type TypeMatch()
    {
        return typeof(InventoryBehaviour);
    }

    public override void DisplayInventory(Vector2Int worldPos, Block _, BlockState state, IInventoryContainer otherInventory)
    {
        _attachedInv = state.GetState<InventoryBehaviourState>().StoredItems;
        singleInventoryDisplay.AttachInv(_attachedInv);
    }

    public override void Detach()
    {
        singleInventoryDisplay.DetachInv(_attachedInv);
    }
}