using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tool : DurableItem, IDamageItem
{
    [Header("Tool Settings")]
    public float Reach;
    public int m_Damage;

    public int Damage => m_Damage;

    protected bool CanReach(Vector3 usePosition, Vector3 targetPosition)
    {
        return Vector2.Distance(usePosition.ToVector2(), targetPosition.ToVector2()) < Reach;
    }

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (PerformAction(usePosition, targetPosition, useInfo) && MaxDurability > 0)
        {
            (useInfo.state as IDurableState)?.Durability.ChangeDurability(-1);
        }
        base.Use(usePosition, targetPosition, useInfo);
    }

    public abstract bool PerformAction(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo);
}
