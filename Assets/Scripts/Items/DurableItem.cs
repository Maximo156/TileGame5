using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DurableItem : Item
{
    public int MaxDurability;

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (MaxDurability > 0)
        {
            (useInfo.state as IDurableState)?.Durability.ChangeDurability(-1);
        }
        base.Use(usePosition, targetPosition, useInfo);
    }
}

public interface IDurableState
{
    public DurableState Durability { get; }
}

public class DurableState
{
    public int CurDurability { get; private set; }
    Action onStateChange;
    public DurableState(int durability, Action onStateChange)
    {
        CurDurability = durability;
        this.onStateChange = onStateChange;
    }

    public void ChangeDurability(int dif)
    {
        CurDurability += dif;

        onStateChange?.Invoke();
    }
}
