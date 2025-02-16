using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileCharm", menuName = "Inventory/Charms/ProjectileCharm", order = 1)]
public class ProjectileCharm : ItemCharm
{
    public Projectile Projectile;

    public override bool IsExclusive => false;
}
