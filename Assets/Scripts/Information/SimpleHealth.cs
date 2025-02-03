using UnityEngine;
using EntityStatistics;

public class SimpleHealth : EntityVariableStat, IHittable
{
    HitIngress hitIngress;
    protected override void Start()
    {
        base.Start();
        hitIngress = GetComponent<HitIngress>();
    }

    protected override void OnChangeStat()
    {
        if (current <= 0)
        {
            SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        }
    }

    protected override void OnTick()
    {
        base.OnTick();
        var damageOverTime = stats.GetStat(EntityStats.Stat.DamageOverTime);
        if (damageOverTime > 0)
        {
            hitIngress.Hit(new HitData() { Damage = damageOverTime });
        }
    }

    public void Hit(HitData data)
    {
        ChangeStat(-data.Damage);
    }
}
