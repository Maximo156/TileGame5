using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public override string GetStatsString()
    {
        var stats = projectile.ReadStats();
        return string.Join('\n', stats.OrderBy(kvp => kvp.Key)
                                  .Where(kvp => kvp.Value != null)
                                  .Select(s => s.Key.ToString().SplitCamelCase() + ": " + s.Value));
    }
}
