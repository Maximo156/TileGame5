using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
    public delegate void StatsChanged(Stat type);
    public event StatsChanged OnStatsChanged;

    public enum Stat
    {
        Health,
        HealthRegen,
        Mana,
        ManaRegen,
        Hunger,
        HungerDepletion,
        DamageModifier,
        Defense,
        MovementModifier,
    }

    public BaseStats baseStats;

    public void AttachInv(Inventory inv)
    {

    }

    public float GetStat(Stat type)
    {
        return baseStats.GetStat(type);
    }
}
