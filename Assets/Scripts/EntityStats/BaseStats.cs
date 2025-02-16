using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EntityStatistics
{
    [CreateAssetMenu(fileName = "NewBaseStats", menuName = "Stats/BaseStats", order = 1)]
    public class BaseStats : ScriptableObject
    {
        [Header("Bar Stats")]
        public float MaxHealth;
        public float MaxHunger;
        public float MaxMana;

        [Header("Value Stats")]
        public float DamageModifier = 0;
        public float Defense = 0;
        public float MovementModifier;

        public float HealthRegen;
        public float ManaRegen;
        public float HungerDepletion;
        public float PickupRange;

        public float GetStat(EntityStats.Stat type)
        {
            switch (type)
            {
                case EntityStats.Stat.Health:
                    return MaxHealth;
                case EntityStats.Stat.Hunger:
                    return MaxHunger;
                case EntityStats.Stat.Mana:
                    return MaxMana;
                case EntityStats.Stat.DamageModifier:
                    return DamageModifier;
                case EntityStats.Stat.Defense:
                    return Defense;
                case EntityStats.Stat.MovementModifier:
                    return MovementModifier;
                case EntityStats.Stat.Healing:
                    return HealthRegen;
                case EntityStats.Stat.ManaRegen:
                    return ManaRegen;
                case EntityStats.Stat.HungerDepletion:
                    return HungerDepletion;
                case EntityStats.Stat.PickupRange:
                    return PickupRange;
                case EntityStats.Stat.DamageOverTime:
                    return 0;
                default:
                    throw new InvalidOperationException("Unknown Stat Type");
            }
        }
    }
}
