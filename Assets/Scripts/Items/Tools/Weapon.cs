using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Inventory/Weapon", order = 1)]
public class Weapon : DurableItem, IColliderListener, IDamageItem
{
    public enum WeaponType
    {
        Sword,
        Bow,
        CrossBow,
        Staff
    }

    [Header("Weapon Info")]
    public WeaponType Type;
    public int CharmSlots;
    public int m_Damage;
    [Stat("ManaModifier", defaultValue: 1)]
    public float ManaModifier = 1;

    [Header("Projectile Modifiers")]
    [Stat("ProjectileScale")]
    public float projectileScale;
    [Stat("ProjectileSpeed")]
    public float projectileSpeed;


    public int Damage => m_Damage;

    public void OnCollision(CollisionInfo info)
    {
        if (MaxDurability > 0)
        {
            (info.state as IDurableState)?.Durability.ChangeDurability(-1);
        }
    }

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        base.Use(usePosition, targetPosition, useInfo);
        if (useInfo.state is not InfusableState infusableState) return;
        var stages = infusableState.stages;
        var cost = infusableState.Cost * ManaModifier;
        
        if (useInfo.UserInfo.Mana.current >= cost)
        {
            useInfo.UserInfo.Mana.ChangeStat(-cost);
            
            FireProjectile(usePosition, targetPosition - usePosition, useInfo);
        }
        foreach (var stage in stages)
        {
            Debug.Log($"Used {stage}");
        }
    }

    public override ItemState GetItemState()
    {
        return new InfusableState(this, CharmSlots);
    }

    protected void FireProjectile(Vector3 usePosition, Vector3 dir, UseInfo useInfo)
    {
        FiredProjectileInfo modifier = FiredProjectileInfo.one;
        modifier.IgnoreColliders.Add(useInfo.ignoreCollider);
        modifier.WeaponDamage = Damage;
        modifier.WeaponScale = projectileScale;
        modifier.WeaponSpeed = projectileSpeed;
        modifier.Stages = GetStages(useInfo);

        if (modifier.Stages != null)
        {
            ProjectileManager.FireStages(usePosition, dir, modifier, useInfo.UserInfo.transform);


            if (MaxDurability > 0)
            {
                (useInfo.state as IDurableState)?.Durability.ChangeDurability(-1);
            }
        }

    }

    protected virtual List<Stage> GetStages(UseInfo useInfo)
    {
        if (useInfo.state is InfusableState infusableState)
        {
            var stages = infusableState.stages;
            var firstStage = stages.FirstOrDefault();
            if (!stages.Any(s => s.Projectile is not null)) return null;
            List<Stage> res;
            if (firstStage?.Projectile is not null)
            {
                res = new List<Stage>(stages.Prepend(null));
            }
            else
            {
                res = new List<Stage>(stages);
            }
            return res;
        };
        return null;
    }
}

public class InfusableState : ItemState, IItemInventoryState, IDurableState, IGridSource
{
    public ItemInventoryState Inventory { get; }

    public DurableState Durability { get; }

    public List<Stage> stages = new List<Stage>();

    public InfusableState(DurableItem durability, int charmSlots)
    {
        Inventory = new ItemInventoryState(CharmAllowedInSlot, charmSlots);
        Durability = new DurableState(durability, this);
    }

    bool CharmAllowedInSlot(Item item, ItemStack[] items, int index)
    {
        if(item is not ItemCharm charm || (charm.IsExclusive && items.Any(i => i?.GetType() == charm.GetType())))
        {
            return false;
        }
        if(item is ProjectileCharm && index != 0 && items[index - 1]?.Item is not ProjectileSplitCharm)
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

    public override string GetStateString(Item item)
    {
        var weapon = item as Weapon;
        var cost = Cost * (weapon?.ManaModifier ?? 1);
        if(cost != 0)
        {
            return $"Mana Cost: {cost}";
        }
        return "";
    }

    public bool Validate()
    {
        var items = Inventory.inv.GetAllItems(true).ToArray();
        return items.Select((item, i) => item == null || CharmAllowedInSlot(item.Item, items, i)).All(b => b);
    }

    public void UpdateStages()
    {
        var charms = Inventory.inv.GetAllItems(false).Select(i => i.Item);
        stages = new List<Stage>();

        ProjectileCharm currentProjectile = null;
        Stage currentStage = new Stage();
        foreach (var charm in charms)
        {
            if(charm is ProjectileSplitCharm split)
            {
                currentStage.split = split;
                stages.Add(currentStage);
                currentStage = new Stage()
                {
                    Projectile = currentProjectile?.Projectile
                };
                
            }
            else if(charm is ProjectileCharm proj)
            {
                currentProjectile = proj;
                currentStage.Projectile = proj.Projectile;
            }
            else if(charm is TargetingCharm targeting)
            {
                currentStage.Targeting = targeting;
            }
            else if(charm is StatCharm stat)
            {
                currentStage.Modifiers.Add(stat);
            }
        }
        stages.Add(currentStage);
    }

    public IEnumerable<IGridItem> GetGridItems() => Inventory.inv.GetAllItems(false);

    public List<ItemCharm> Charms => Inventory.inv.GetAllItems(false).Select(i => i.Item as ItemCharm).ToList();

    public float Cost => Charms.Sum(c => c.manaCost) * (1 + Charms.Sum(c => c.CostModifier));
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


