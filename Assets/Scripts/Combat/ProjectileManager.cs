using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ProjectileInfo
{
    public float damageMultiplier;
    public float speedMultiplier;
    public float scaleMultiplier;
    public float powerModifier;
    public List<ItemCharm> Charms;
    public Collider2D UserCollider;

    public static ProjectileInfo one = new ProjectileInfo()
    {
        damageMultiplier = 1,
        speedMultiplier = 1,
        scaleMultiplier = 1,
        powerModifier = 1,
        Charms = new()
    };
}

public class ProjectileManager : MonoBehaviour
{
    public int maxProjectiles;
    public int ProjectileRenderLayer = 100;
    Queue<ProjectileEntity> ExistingProjectiles = new Queue<ProjectileEntity>();

    public static void FireProjectile(Projectile projectileBase, Vector2 position, Vector2 dir, ProjectileInfo modifier, Transform perp, Transform target = null)
    {
        ProjectileEntity projEntity;
        ProjectileManager manager = ChunkManager.CurRealm.EntityContainer.ProjectileManager;
        if (manager.ExistingProjectiles.Count >= manager.maxProjectiles)
        {
            projEntity = manager.ExistingProjectiles.Dequeue();
            projEntity.gameObject.SetActive(true);
        }
        else
        {
            projEntity = new GameObject("projectile").AddComponent<ProjectileEntity>();
            projEntity.transform.parent = manager.transform;
        }
        projEntity.Setup(projectileBase, position, dir, modifier, manager.ProjectileRenderLayer, perp, target);
        manager.ExistingProjectiles.Enqueue(projEntity);
    }
}
