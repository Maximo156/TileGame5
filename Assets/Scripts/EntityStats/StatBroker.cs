using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EntityStatistics
{ 

    public class StatBroker: IGridSource
    {
        public event Action<EntityStats.Stat> OnStatChanged;

        Dictionary<EntityStats.Stat, float> CachedStats = new();

        LinkedList<Modifier> TimedModifiers = new();
        LinkedList<Modifier> AllModifiers = new();

        Dictionary<Item, List<Modifier>> AccessoryModifiers = new();
        AccessoryInv m_AccessoryInv;

        Action<Query> Queries = delegate { };

        public void AttachInv(AccessoryInv AccessoryInv)
        {
            if (m_AccessoryInv != null)
            {
                m_AccessoryInv.OnItemChanged -= OnInvChange;
            }
            m_AccessoryInv = AccessoryInv;
            m_AccessoryInv.OnItemChanged += OnInvChange;
            OnInvChange(m_AccessoryInv);
        }

        void OnInvChange(Inventory _)
        {
            var items = m_AccessoryInv.GetAllItems(false).Select(i => i.Item as Accessory);
            var newModifiers = new Dictionary<Item, List<Modifier>>();
            foreach(var item in items)
            {
                if(AccessoryModifiers.TryGetValue(item, out var found))
                {
                    newModifiers[item] = found;
                    AccessoryModifiers.Remove(item);
                }
                else
                {
                    var modifiers = item.GenerateModifiers().ToList();
                    newModifiers[item] = modifiers;
                    ApplyModifiers(modifiers);
                }
            }
            foreach(var modifier in AccessoryModifiers.SelectMany(kvp => kvp.Value))
            {
                modifier.Dispose();
            }
            AccessoryModifiers = newModifiers;
        }

        public void ApplyModifiers(List<Modifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                ApplyModifier(modifier);
            }
        }

        public float GetStat(EntityStats.Stat type, float baseValue)
        {
            if(CachedStats.TryGetValue(type, out var val))
            {
                return val;
            }

            float addative = 0;
            float mutliplicative = 1;
            var node = AllModifiers.First;
            while (node != null)
            {
                var modifier = node.Value;
                if (modifier.StatType == type)
                {
                    if (modifier.method == Modifier.Method.Addative)
                    {
                        addative += modifier.value;
                    }
                    else
                    {
                        mutliplicative += modifier.value;
                    }
                }
                node = node.Next;
            }
            var res = (baseValue + addative) * mutliplicative;
            CachedStats[type] = res;
            return res;
        }

        public void Update()
        {
            var node = TimedModifiers.First;
            while (node != null)
            {
                var modifier = node.Value;
                if (modifier.Expired)
                {
                    modifier.Dispose();
                }
                node = node.Next;
            }
        }

        public void ApplyModifier(Modifier modifier)
        {
            Queries += modifier.Handle;
            var node = AllModifiers.AddLast(modifier);
            modifier.OnDispose += () =>
            {
                Queries -= modifier.Handle;
                CachedStats.Remove(modifier.StatType);
                AllModifiers.Remove(node);
                OnStatChanged?.Invoke(modifier.StatType);
            };

            if (modifier.Timed)
            {
                var timedNode = TimedModifiers.AddLast(modifier);
                modifier.OnDispose += () => TimedModifiers.Remove(timedNode);
            }

            CachedStats.Remove(modifier.StatType);
            OnStatChanged?.Invoke(modifier.StatType);
        }

        public IEnumerable<IGridItem> GetGridItems() => AllModifiers.Where(m => m.isDisplayable);
    }

    public class Query
    {
        public EntityStats.Stat type;
        public float value;
    }
}
