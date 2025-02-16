using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemCharm : Item
{
    public Weapon.WeaponType? WeaponType;
    [Stat("ManaCost")]
    public float manaCost;
    [Stat("CostModifier")]
    public float CostModifier = 0;

    public abstract bool IsExclusive { get; }
}
