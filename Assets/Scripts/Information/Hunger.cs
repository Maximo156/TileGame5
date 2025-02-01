using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hunger : EntityVariableStat
{
    public float DamageWhenStarving;

    protected override void OnChangeStat()
    {
        
    }

    protected override void OnTick()
    {
        base.OnTick();
        if(current <= 0)
        {
            SendMessage("Damage", DamageWhenStarving, SendMessageOptions.DontRequireReceiver);
        }
    }
}
