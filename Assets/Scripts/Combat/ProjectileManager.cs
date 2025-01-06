using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ProjectileModifier
{
    public float damageMultiplier;
    public float speedMultiplier;
    public float scaleMultiplier;
    public float powerModifier;

    public static ProjectileModifier one = new ProjectileModifier()
    {
        damageMultiplier = 1,
        speedMultiplier = 1,
        scaleMultiplier = 1,
        powerModifier = 1
    };
}

public class ProjectileManager : MonoBehaviour
{
    public int maxProjectiles;
    Queue<ProjectileEntity> ExistingProjectiles = new Queue<ProjectileEntity>();
    void Awake()
    {
        //ServiceManager.Register<ProjectileManager, ProjectileManager>(this);
    }

    public void FireProjectile(Projectile projectileBase, Vector2 position, Vector2 dir, ProjectileModifier modifier, Transform target = null)
    {
        ProjectileEntity projEntity;
        if(ExistingProjectiles.Count >= maxProjectiles)
        {
            projEntity = ExistingProjectiles.Dequeue();
            projEntity.gameObject.SetActive(true);
        }
        else
        {
            projEntity = new GameObject("projectile").AddComponent<ProjectileEntity>();
            projEntity.transform.parent = transform;
        }
        projEntity.Setup(projectileBase, position, dir, modifier, target);
        ExistingProjectiles.Enqueue(projEntity);
    }
}
