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
    [Stat(Stat.StatType.WeaponType)]
    public WeaponType Type;

    public void OnCollision(CollisionInfo info)
    {
        if (MaxDurability > 0)
        {
            (info.state as IDurableState)?.Durability.ChangeDurability(-1);
        }
    }
}


