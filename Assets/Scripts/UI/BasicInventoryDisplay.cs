using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicInventoryDisplay : InteractableDislay
{
    public SingleInventoryDisplay basicDisplay;
    public int MaxDisplayWidth = 5;

    Inventory attachedInv;
    public override void DisplayInventory(Vector2 worldPos, IInteractableState state)
    {
        /*var inv = (state as IInventoryState).GetInv();
        basicDisplay.display.constraintCount = Mathf.Min(MaxDisplayWidth, inv.Count);
        basicDisplay.AttachInv(inv);
        attachedInv = inv;*/
    }

    public override void Detach()
    {
        basicDisplay.DetachInv(attachedInv);
    }

    public override Type TypeMatch()
    {
        return typeof(BasicInventoryDisplay);
    }
}
