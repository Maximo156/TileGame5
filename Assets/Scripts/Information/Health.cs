using UnityEngine;
using EntityStatistics;

public class Health : EntityVariableStat
{
    public Hunger hunger;
    public int HungerPerRegen;

    protected override float Regen => hunger is null || hunger.current > hunger.MaxValue /2 ? base.Regen : 0;

    protected override void OnChangeStat()
    {
        if (current <= 0)
        {
            SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        }
    }

    protected override void OnTick()
    {
        if(current < MaxValue && Regen > 0 && hunger is not null)
        {
            hunger.ChangeStat(-HungerPerRegen);
        }
        base.OnTick();
        var damageOverTime = stats.GetStat(EntityStats.Stat.DamageOverTime);
        if (damageOverTime > 0)
        {
            SendMessage("Damage", damageOverTime, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void Damage(float damage)
    {
        ChangeStat(-damage);
    }
}
