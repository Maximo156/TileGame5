using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInfusableWeapon", menuName = "Inventory/InfusableWeapon", order = 1)]
public class InfusableWeapon : Weapon
{
    public int CharmSlots;

    public override ItemState GetItemState()
    {
        return new InfusableState(MaxDurability, CharmSlots);
    }
}
public class InfusableState : ItemState, IItemInventoryState, IDurableState
{
    public ItemInventoryState Inventory { get; }

    public DurableState Durability { get; }

    public InfusableState(int durability, int charmSlots)
    {
        Inventory = new ItemInventoryState(i => i is ItemCharm, charmSlots);
        Durability = new DurableState(durability, this);
    }
}
