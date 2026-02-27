using ComposableBlocks;
using NativeRealm;
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
            curState.OnStateUpdated -= SetSliders;
            Inputs.DetachInv(curState.inputs);
            Outputs.DetachInv(curState.outputs);
            Fuels.DetachInv(curState.fuels);
        }
    }

    ProcessingBlockBehaviourState curState;
    public override void DisplayInventory(Vector2Int worldPos, Block _, BlockState state, IInventoryContainer otherInventory)
    {
        curState = state.GetState<ProcessingBlockBehaviourState>();
        curState.OnStateUpdated += SetSliders;
        Render();
    }

    public void Render()
    {
        Inputs.AttachInv(curState.inputs);
        Outputs.AttachInv(curState.outputs);

        if (curState.requiresFuel)
        {
            Fuels.gameObject.SetActive(true);
            Fuels.AttachInv(curState.fuels);
        }
        else
        {
            Fuels.gameObject.SetActive(false);
        }

        SetSliders();
    }

    void SetSliders()
    {
        CompletionSlider.fillAmount =  1 - (curState.timeLeft / (curState.curRecipe?.craftingTime * 1000)) ?? 0;

        if (curState.requiresFuel)
        {
            FuelConsumptionSlider.gameObject.SetActive(true);
            FuelConsumptionSlider.fillAmount = (curState.curFuel / curState.lastUsedFuel) ?? 0;
        }
        else
        {
            FuelConsumptionSlider.gameObject.SetActive(false);
        }
    }

    public override Type TypeMatch()
    {
        return typeof(ProcessingBlockBehaviour);
    }
}
