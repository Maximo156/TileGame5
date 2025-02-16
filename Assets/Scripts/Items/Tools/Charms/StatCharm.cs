using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStatCharm", menuName = "Inventory/Charms/StatCharm", order = 1)]
public class StatCharm : ItemCharm
{
    [Stat("DamageModifier")]
    public float DamageModifier;

    public override bool IsExclusive => false;
}
