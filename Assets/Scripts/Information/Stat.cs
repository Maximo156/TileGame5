using System;

[AttributeUsage(AttributeTargets.Field)]
public class Stat : Attribute
{
    public enum StatType
    {
        Defense,
        Damage,
        Hardness,
        Efficiency,
        Range,
        Peircing,
        ManaRegen,
        HealthRegen,
        ManaCost,
        CostModifier,
        Healing,
        Food,
        ManaRestoration,
        WeaponType
    }

    private StatType type;

    public Stat(StatType type)
    {
        this.type = type;
    }

    public StatType GetStatType() => type;
}
