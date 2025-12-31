using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hunger : EntityVariableStat
{
    public float DamageWhenStarving;

    HitIngress hitIngress;
    protected override void Start()
    {
        base.Start();
        hitIngress = GetComponent<HitIngress>();
    }

    protected override void OnChangeStat()
    {
        
    }

    protected override void OnTick()
    {
        base.OnTick();
        if(current <= 0)
        {
            hitIngress.Hit(new HitData() { Damage = DamageWhenStarving, Perpetrator = this.transform });
        }
    }
}
