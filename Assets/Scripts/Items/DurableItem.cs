using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DurableItem : Item
{
    public int MaxDurability;

    public override ItemState GetItemState()
    {
        return new DurableState(this, null);
    }
}

public interface IDurableState
{
    public DurableState Durability { get; }
}

public class DurableState : ItemState, IDurableState
{
    public int CurDurability { get; private set; }
    public int MaxDurability => item.MaxDurability;

    public DurableState Durability => this;

    readonly DurableItem item;
    readonly Action onStateChange;
    public DurableState(DurableItem item, ItemState containingState)
    {
        CurDurability = item.MaxDurability;
        this.item = item;
        onStateChange = containingState is not null ? containingState.TriggerStateChange : TriggerStateChange;
    }

    public void ChangeDurability(int dif)
    {
        CurDurability += dif;

        onStateChange?.Invoke();
    }
}
