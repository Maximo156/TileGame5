using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Inventory/Weapon", order = 1)]
public class Weapon : DurableItem
{
    public enum WeaponType
    {
        Sword,
        Bow,
        CrossBow,
        Staff
    }

    [Header("Weapon Info")]
    [Stat(Stat.StatType.WeaponType)]
    public WeaponType Type;

    public int CharmSlots;

    public override ItemState GetItemState()
    {
        return new ItemInventoryState(i => i is ItemCharm charm && (charm.WeaponType is null || charm.WeaponType == Type), CharmSlots);
    }
}
