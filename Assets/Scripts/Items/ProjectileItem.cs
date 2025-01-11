using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileItem", menuName = "Inventory/ProjectileItem", order = 1)]
public class ProjectileItem : Item
{
    public enum ProjectileType
    {
        Arrow,
        Bolt,
        Bullet
    }
    public Projectile projectile;
    public ProjectileType Type;
}
