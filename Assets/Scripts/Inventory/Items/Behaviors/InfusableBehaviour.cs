using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InfusableBehaviour : ItemBehaviour, IStatefulItemBehaviour
{
    public int charmSlots;

    public ItemBehaviourState GetNewState()
    {
        return new InfusableBehaviourState(charmSlots);
    }
}

public class InfusableBehaviourState : ItemInventoryBehaviourState, IStateStringProvider
{
    [JsonIgnore]
    public List<Stage> stages = new List<Stage>();

    public int charmSlots;
    public InfusableBehaviourState(int charmSlots) : base(CharmAllowedInSlot, charmSlots)
    { 
        this.charmSlots = charmSlots;
    }

    [JsonConstructor]
    public InfusableBehaviourState(int charmSlots, Inventory inv) : base(CharmAllowedInSlot, charmSlots)
    {
        this.charmSlots = charmSlots;
        inv.TransferToInventory(this.inv);
        UpdateStages();
    }

    public override IEnumerable<IGridItem> GetGridItems() => inv.GetAllItems(false);

    public string GetStateString(Item item)
    {
        item.GetBehavior<WeaponBehaviour>(out var weapon);
        var cost = Cost * (weapon?.ManaModifier ?? 1);
        if (cost != 0)
        {
            return $"Mana Cost: {cost}";
        }
        return "";
    }

    public bool Validate()
    {
        var items = inv.GetAllItems(true).ToArray();
        return items.Select((item, i) => item == null || CharmAllowedInSlot(item.Item, items, i)).All(b => b);
    }

    public void UpdateStages()
    {
        var charms = inv.GetAllItems(false).Select(i => i.Item);
        stages = new List<Stage>();

        ProjectileCharm currentProjectile = null;
        Stage currentStage = new Stage();
        foreach (var charm in charms)
        {
            if (charm is ProjectileSplitCharm split)
            {
                currentStage.split = split;
                stages.Add(currentStage);
                currentStage = new Stage()
                {
                    Projectile = currentProjectile?.Projectile
                };

            }
            else if (charm is ProjectileCharm proj)
            {
                currentProjectile = proj;
                currentStage.Projectile = proj.Projectile;
            }
            else if (charm is TargetingCharm targeting)
            {
                currentStage.Targeting = targeting;
            }
            else if (charm is StatCharm stat)
            {
                currentStage.Modifiers.Add(stat);
            }
        }
        stages.Add(currentStage);
    }

    [JsonIgnore]
    public List<ItemCharm> Charms => inv.GetAllItems(false).Select(i => i.Item as ItemCharm).ToList();

    [JsonIgnore]
    public float Cost => Charms.Sum(c => c.manaCost) * (1 + Charms.Sum(c => c.CostModifier));

    static bool CharmAllowedInSlot(Item item, ItemStack[] items, int index)
    {
        if (item is not ItemCharm charm || (charm.IsExclusive && items.Any(i => i?.GetType() == charm.GetType())))
        {
            return false;
        }
        if (item is ProjectileCharm && index != 0 && items[index - 1]?.Item is not ProjectileSplitCharm)
        {
            return false;
        }
        if (item is ProjectileSplitCharm && index != 0 && !items.Take(index).Any(i => i?.Item is ProjectileCharm))
        {
            return false;
        }
        if (item is TargetingCharm && (index == 0 || items[index - 1]?.Item is not ProjectileCharm and not ProjectileSplitCharm))
        {
            return false;
        }
        return true;
    }
}

public class Stage
{
    public Projectile Projectile;
    public TargetingCharm Targeting;
    public ProjectileSplitCharm split;

    public List<StatCharm> Modifiers = new();

    public override string ToString()
    {
        return $"{Projectile?.name} {Targeting?.name} {split?.name}: {string.Join(",", Modifiers.Select(m => m.name))}";
    }
}
