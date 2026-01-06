using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileSplitCharm", menuName = "Inventory/Charms/ProjectileSplitCharm", order = 1)]
public class ProjectileSplitCharm : ItemCharm
{
    [Stat("Projectile Count")]
    public int splitCount;
    [Stat("Damage Modifier")]
    public float splitDamageModifier;
    [Stat("Spread Angel")]
    public float SpreadAngle;
    [Stat("Split Time", "On Contact")]
    public float secondsBeforeSplit;

    public override bool IsExclusive => false;
}
