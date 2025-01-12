using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Inventory/Weapon", order = 1)]
public class Weapon : DurableItem, IColliderListener
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
        var charms = infusableState.Charms;
        var cost = infusableState.Cost;
        var proj = charms.FirstOrDefault(c => c is ProjectileCharm) as ProjectileCharm;
        if(proj != null)
        {
            if (useInfo.UserInfo.Mana.current >= cost)
            {
                useInfo.UserInfo.Mana.ChangeStat(-cost);
                FireProjectile(usePosition, targetPosition, useInfo, proj.Projectile);
            }
        }
        foreach (var charm in charms)
        {
            Debug.Log($"Used {charm.name}");
        }
    }

    public override ItemState GetItemState()
    {
        return new InfusableState(this, CharmSlots);
    }

    protected void FireProjectile(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo, Projectile projectile)
    {
        ProjectileInfo modifier = ProjectileInfo.one;
        modifier.UserCollider = useInfo.ignoreCollider;
        if (useInfo.state is InfusableState infusableState)
        {
            modifier.Charms = infusableState.Charms;
        };
        ProjectileManager.FireProjectile(projectile, usePosition, targetPosition - usePosition, modifier, useInfo.UserInfo.transform);
        if (MaxDurability > 0)
        {
            (useInfo.state as IDurableState)?.Durability.ChangeDurability(-1);
        }
    }
}

public class InfusableState : ItemState, IItemInventoryState, IDurableState, IGridSource
{
    public ItemInventoryState Inventory { get; }

    public DurableState Durability { get; }

    public InfusableState(DurableItem durability, int charmSlots)
    {
        Inventory = new ItemInventoryState(
        (i, inv) =>
        {
            return i is ItemCharm charm && 
            (!charm.Exclusive || !inv.GetAllItems(false).Any(i => i.Item.GetType() == charm.GetType())) &&
            (durability is ProjectileWeapon != charm is ProjectileCharm);
        }
        , charmSlots);
        Durability = new DurableState(durability, this);
    }

    public override string GetStateString()
    {
        var cost = Cost;
        if(cost != 0)
        {
            return $"Mana Cost: {cost}";
        }
        return "";
    }

    public IEnumerable<IGridItem> GetGridItems() => Inventory.inv.GetAllItems(false);

    public List<ItemCharm> Charms => Inventory.inv.GetAllItems(false).Select(i => i.Item as ItemCharm).ToList();

    public float Cost => Charms.Sum(c => c.manaCost) * (1 + Charms.Sum(c => c.CostModifier));
}


