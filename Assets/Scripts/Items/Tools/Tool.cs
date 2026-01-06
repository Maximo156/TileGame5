using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tool : Item
{
    //[Header("Tool Settings")]
    //public float Reach;
    //public int m_Damage;

    //public int Damage => m_Damage;

    protected bool CanReach(Vector3 usePosition, Vector3 targetPosition)
    {
        return true;// Vector2.Distance(usePosition.ToVector2(), targetPosition.ToVector2()) < Reach;
    }

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (PerformAction(usePosition, targetPosition, useInfo) && useInfo.stack.GetState<DurabilityState>(out var state))
        {
            state.ChangeDurability(-1);
        }
        base.Use(usePosition, targetPosition, useInfo);
    }

    public abstract bool PerformAction(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo);
}
