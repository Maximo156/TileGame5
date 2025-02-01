using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EntityStatistics
{
    public class EntityStats : MonoBehaviour, IGridSource
    {
        public event Action<Stat> OnStatChanged;

        public enum Stat
        {
            Health,
            Healing,
            Mana,
            ManaRegen,
            Hunger,
            HungerDepletion,
            DamageModifier,
            Defense,
            MovementModifier,
            DamageOverTime
        }

        public BaseStats baseStats;

        StatBroker broker;
        private void Awake()
        {
            broker = new StatBroker();
            broker.OnStatChanged += StatsChanged;
        }

        public void AttachInv(AccessoryInv inv)
        {
            broker.AttachInv(inv);
        }

        public float GetStat(Stat type)
        {
            return broker.GetStat(type, baseStats.GetStat(type));
        }

        public void Update()
        {
            broker.Update();
        }

        public void ApplyModifiers(IEnumerable<Modifier> modifiers)
        {
            broker.ApplyModifiers(modifiers.ToList());
        }

        void StatsChanged(Stat type)
        {
            OnStatChanged?.Invoke(type);
        }

        public IEnumerable<IGridItem> GetGridItems() => broker.GetGridItems();
    }
}
