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
        return typeof(StorageBlock);
    }

    public override void DisplayInventory(Vector2Int worldPos, Wall _, BlockState state, IInventoryContainer otherInventory)
    {
        _attachedInv = (state as StorageState).StoredItems;
        singleInventoryDisplay.AttachInv(_attachedInv);
    }

    public override void Detach()
    {
        singleInventoryDisplay.DetachInv(_attachedInv);
    }
}