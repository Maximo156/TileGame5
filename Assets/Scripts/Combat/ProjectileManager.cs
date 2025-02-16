using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct FiredProjectileInfo
{
    public float WeaponDamage;
    public float WeaponSpeed;
    public float WeaponScale;
    public int Stage;
    public List<Stage> Stages;
    public List<Collider2D> IgnoreColliders;

    public FiredProjectileInfo MoveNextStage() => new FiredProjectileInfo
    {
        WeaponDamage = WeaponDamage,
        WeaponScale = WeaponScale,
        WeaponSpeed = WeaponSpeed,
        Stages = Stages,
        IgnoreColliders = new List<Collider2D>(IgnoreColliders),
        Stage = Stage + 1
    };

    public Stage CurStage() => Stage < Stages.Count ? Stages[Stage] : null;

    public static FiredProjectileInfo one = new FiredProjectileInfo()
    {
        WeaponDamage = 0,
        WeaponSpeed = 1,
        WeaponScale = 1,
        Stages = new(),
        IgnoreColliders = new()
    };
}

public class ProjectileManager : MonoBehaviour
{
    public int maxProjectiles;
    public int ProjectileRenderLayer = 100;
    Queue<ProjectileEntity> ExistingProjectiles = new Queue<ProjectileEntity>();

    public static void FireProjectile(Projectile projectileBase, Vector2 position, Vector2 dir, FiredProjectileInfo modifier, Transform perp, Transform target = null)
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

    public static void FireStages(Vector2 position, Vector2 dir, FiredProjectileInfo modifier, Transform perp, Transform target = null)
    {
        var splitStage = modifier.CurStage();
        modifier = modifier.MoveNextStage();
        var curStage = modifier.CurStage();
        if(curStage is null)
        {
            return;
        }

        int count = splitStage?.split?.splitCount ?? 1;
        var spread = splitStage?.split?.SpreadAngle ?? 0;
        var proj = curStage.Projectile;
        var targeting = curStage.Targeting;
        IEnumerator<Collider2D> targets = null;


        if(targeting is not null && splitStage != null)
        {
            int mask;
            if(perp.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                mask = 1 << LayerMask.NameToLayer("Mobs");
            }
            else
            {
                mask = LayerMask.GetMask("Player", "Mobs");
            }
            var found = Physics2D.OverlapCircleAll(position, targeting.TargetingRange, mask);
            targets = found.Where(t => !modifier.IgnoreColliders.Contains(t)).GetEnumerator();
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 fireDir;
            targets?.MoveNext();
            if (targets?.Current != null)
            {
                fireDir = targets.Current.transform.position.ToVector2() - position;
                targets.MoveNext();
            }
            else
            {
                fireDir = Quaternion.Euler(0, 0, -spread / 2 + (spread / count * i)) * dir;
            }
            FireProjectile(proj, position, fireDir, modifier, perp, target);
        }
    }
}
