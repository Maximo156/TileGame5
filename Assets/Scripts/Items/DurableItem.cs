using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DurableItem : Item
{
    public int MaxDurability;

    public override ItemState GetItemState()
    {
        return new DurableState(MaxDurability, null);
    }
}

public interface IDurableState
{
    public DurableState Durability { get; }
}

public class DurableState : ItemState, IDurableState
{
    public int CurDurability { get; private set; }

    public DurableState Durability => this;

    readonly Action onStateChange;
    public DurableState(int durability, ItemState containingState)
    {
        CurDurability = durability;
        onStateChange = containingState is not null ? containingState.TriggerStateChange : TriggerStateChange;
    }

    public void ChangeDurability(int dif)
    {
        CurDurability += dif;

        onStateChange?.Invoke();
    }
}
