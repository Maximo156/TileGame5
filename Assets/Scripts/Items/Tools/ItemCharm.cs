using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemCharm", menuName = "Inventory/ItemCharm", order = 1)]
public class ItemCharm : Item
{
    public Weapon.WeaponType? WeaponType;
}
