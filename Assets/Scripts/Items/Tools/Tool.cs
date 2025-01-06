using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tool : DurableItem
{
    [Header("Tool Settings")]
    public float Reach;

    protected bool CanReach(Vector3 usePosition, Vector3 targetPosition)
    {
        return Vector2.Distance(usePosition.ToVector2(), targetPosition.ToVector2()) < Reach;
    }
}
