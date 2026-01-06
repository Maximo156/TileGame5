using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UseBehavior : ItemBehaviour
{
    public int priority;
    public virtual bool Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if(UseImpl(usePosition, targetPosition, useInfo))
        {
            if(useInfo.stack.GetState<DurabilityState>(out var dur))
            {
                dur.ChangeDurability(-1);
            }
            return true;
        }
        return false;
    }

    protected abstract bool UseImpl(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo);
}

public abstract class RangedUseBehavior : UseBehavior
{
    public int Reach = 7;

    protected bool CanReach(Vector3 usePosition, Vector3 targetPosition)
    {
        return Vector2.Distance(usePosition.ToVector2(), targetPosition.ToVector2()) < Reach;
    }

    protected abstract bool UseRanged(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo);

    protected override bool UseImpl(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if(CanReach(usePosition, targetPosition))
        {
            return UseRanged(usePosition, targetPosition, useInfo);
        }
        return false;
    }
}
