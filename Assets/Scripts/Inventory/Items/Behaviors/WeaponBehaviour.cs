using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponBehaviour : UseBehavior, ICollisionBehaviour
{
    public enum WeaponType
    {
        Sword,
        Bow,
        CrossBow,
        Staff,
        Spear
    }

    [Header("Weapon Info")]
    public WeaponType Type;
    [Stat("ManaModifier", defaultValue: 1)]
    public float ManaModifier = 1;

    [Header("Projectile Modifiers")]
    [Stat("ProjectileScale")]
    public float projectileScale;
    [Stat("ProjectileSpeed")]
    public float projectileSpeed;

    public virtual void OnCollision(CollisionInfo info)
    {
        if (info.stack.GetState<DurabilityState>(out var state))
        {
            state.ChangeDurability(-1);
        }
    }

    protected override (bool used, bool useDurability) UseImpl(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (!useInfo.stack.GetState<InfusableBehaviourState>(out var infusableState)) return (true, false);
        var cost = infusableState.Cost * ManaModifier;

        if (useInfo.UserInfo.Mana.current >= cost)
        {
            useInfo.UserInfo.Mana.ChangeStat(-cost);

            return FireProjectile(usePosition, targetPosition - usePosition, useInfo);
        }
        return (false, false);
    }

    protected (bool used, bool useDurability) FireProjectile(Vector3 usePosition, Vector3 dir, UseInfo useInfo)
    {
        FiredProjectileInfo modifier = FiredProjectileInfo.one;
        modifier.IgnoreColliders.Add(useInfo.ignoreCollider);
        useInfo.stack.GetBehaviour<DamageBehaviour>(out var damage);
        modifier.WeaponDamage = damage?.Damage ?? 0;
        modifier.WeaponScale = projectileScale;
        modifier.WeaponSpeed = projectileSpeed;
        modifier.Stages = GetStages(useInfo);

        if (modifier.Stages != null)
        {
            ProjectileManager.FireStages(usePosition, dir, modifier, useInfo.UserInfo.transform);
            return (true, true);
        }
        return (false, false);
    }

    protected virtual List<Stage> GetStages(UseInfo useInfo)
    {
        if (useInfo.stack.GetState<InfusableBehaviourState>(out var infusableState))
        {
            var stages = infusableState.stages;
            var firstStage = stages.FirstOrDefault();
            if (!stages.Any(s => s.Projectile is not null)) return null;
            List<Stage> res;
            if (firstStage?.Projectile is not null)
            {
                res = new List<Stage>(stages.Prepend(null));
            }
            else
            {
                res = new List<Stage>(stages);
            }
            return res;
        };
        return null;
    }
}
