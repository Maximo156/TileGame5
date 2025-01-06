using System;
using UnityEngine;
using UnityEngine.UI;

public class ProcessorDisplay : InteractiveDislay
{
    public SingleInventoryDisplay Inputs;
    public SingleInventoryDisplay Fuels;
    public SingleInventoryDisplay Outputs;

    public Image CompletionSlider;
    public Image FuelConsumptionSlider;


    public override void Detach()
    {
        if (curState != null)
        {
            curState.OnStateChange -= SetSliders;
            Inputs.DetachInv(curState.inputs);
            Outputs.DetachInv(curState.outputs);
            Fuels.DetachInv(curState.fuels);
        }
    }

    ProcessingBlockState curState;
    public override void DisplayInventory(Vector2 worldPos, BlockSlice slice, IInventoryContainer otherInventory)
    {
        curState = slice.State as ProcessingBlockState;
        curState.OnStateChange += SetSliders;
        Render();
    }

    public void Render()
    {
        print("attach");
        Inputs.AttachInv(curState.inputs);
        Outputs.AttachInv(curState.outputs);
        Fuels.AttachInv(curState.fuels);

        SetSliders(curState);
    }

    void SetSliders(ProcessingBlockState changedState)
    {
        if (changedState != curState)
        {
            throw new InvalidOperationException("Rendering incorrect state");
        }

        CompletionSlider.fillAmount =  1 - (curState.timeLeft / (curState.curRecipe?.craftingTime * 1000)) ?? 0;
        FuelConsumptionSlider.fillAmount = (curState.curFuel / curState.lastUsedFuel) ?? 0;
    }

    public override Type TypeMatch()
    {
        return typeof(ProcessingBlock);
    }
}
