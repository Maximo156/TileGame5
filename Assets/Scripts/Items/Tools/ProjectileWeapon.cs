using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileWeapon", menuName = "Inventory/ProjectileWeapon", order = 1)]
public class ProjectileWeapon : Weapon
{
    public List<ProjectileItem.ProjectileType> AllowedTypes;

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        var projectileItem = useInfo.availableInventory.GetAllItems(false).FirstOrDefault(i => i.Item is ProjectileItem proj && AllowedTypes.Contains(proj.Type))?.Item as ProjectileItem;
        if (projectileItem is not null)
        {
            FireProjectile(usePosition, targetPosition - usePosition, useInfo);
            useInfo.availableInventory.RemoveItemSafe(new ItemStack(projectileItem, 1));
        }
    }

    protected override List<Stage> GetStages(UseInfo useInfo)
    {
        var projectileItem = useInfo.availableInventory.GetAllItems(false).FirstOrDefault(i => i.Item is ProjectileItem proj && AllowedTypes.Contains(proj.Type)).Item as ProjectileItem;
        if (projectileItem is not null)
        {
            return new List<Stage>(){ null, new Stage() { Projectile = projectileItem.projectile } };
        }
        return null;
    }
}
