using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProjectileWeaponBehaviour : WeaponBehaviour
{
    public List<ProjectileItem.ProjectileType> AllowedTypes;
    public override void OnCollision(CollisionInfo info)
    {}

    protected override (bool used, bool useDurability) UseImpl(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        var projectileItem = useInfo.availableInventory.GetAllItems(false).FirstOrDefault(i => i.Item is ProjectileItem proj && AllowedTypes.Contains(proj.Type))?.Item as ProjectileItem;
        if (projectileItem is not null)
        {
            FireProjectile(usePosition, targetPosition - usePosition, useInfo);
            useInfo.availableInventory.RemoveItemSafe(new ItemStack(projectileItem, 1));
            return (true, true);
        }
        return (true, false);
    }

    protected override List<Stage> GetStages(UseInfo useInfo)
    {
        var projectileItem = useInfo.availableInventory.GetAllItems(false).FirstOrDefault(i => i.Item is ProjectileItem proj && AllowedTypes.Contains(proj.Type)).Item as ProjectileItem;
        if (projectileItem is not null)
        {
            return new List<Stage>() { null, new Stage() { Projectile = projectileItem.projectile } };
        }
        return null;
    }
}
