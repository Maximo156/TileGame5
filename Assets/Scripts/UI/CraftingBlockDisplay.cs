using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingBlockDisplay : InteractableDislay
{
    public SingleInventoryDisplay InputDisplay;
    public SingleInventoryDisplay OutputDisplay;
    public Slider slider;

    //dynamic crafter;
    public override void DisplayInventory(Vector2 worldPos, IInteractableState state)
    {
        //crafter = state as CraftingState;
       // InputDisplay.AttachInv(crafter.stacks);
        //OutputDisplay.AttachInv(crafter.produced);
        SetSldier();
    }

    public override void Detach()
    {
        /*
        if (crafter != null)
        {
            InputDisplay.DetachInv(crafter.stacks);
            OutputDisplay.DetachInv(crafter.produced);
        }
        crafter = null;*/
    }

    public void FixedUpdate()
    {
        SetSldier();
    }

    private void SetSldier()
    {
        /*
        if (crafter != null)
        {
            if (crafter.cachedProduced != null)
            {
                slider.value = 1 - crafter.ticksLeft * 1f / crafter.cachedProduced.craftingTimeTicks;
            }
            else
            {
                slider.value = 0;
            }
        }*/
    }

    public override Type TypeMatch()
    {
        return typeof(CraftingBlockDisplay);
    }
}