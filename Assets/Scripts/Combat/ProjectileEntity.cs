using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProjectileEntity : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sr;

    PolygonCollider2D col;


    // ProjectileInfo;
    Transform Perpetrator;
    float damage;
    float AOE;
    float pierces;
    AnimatedProjectile.AnimationInfo HitAnimation;
    FiredProjectileInfo info;

    void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        sr = gameObject.AddComponent<SpriteRenderer>();
        gameObject.layer = LayerMask.NameToLayer("Projectile");
    }

    public void Setup(Projectile projectileBase, Vector2 position, Vector2 dir, FiredProjectileInfo projectileInfo, int renderLayer, Transform perpetrator, Transform target = null)
    {
        info = projectileInfo;
        transform.localScale = projectileBase.scale * projectileInfo.WeaponScale * Vector3.one;
        transform.position = position;
        transform.right = dir;
        rb.velocity = projectileBase.speed * projectileInfo.WeaponSpeed * dir.normalized;
        damage = projectileBase.damage + projectileInfo.WeaponDamage;
        sr.sprite = projectileBase.sprite;
        sr.color = projectileBase.color;
        sr.sortingOrder = renderLayer;
        col = gameObject.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;
        Perpetrator = perpetrator;
        AOE = projectileBase.AOE;
        pierces = projectileBase.peirce;
        if (projectileBase is AnimatedProjectile animatedProj)
        {
            HitAnimation = animatedProj.HitAnimation;
            if (animatedProj.FlyingAnimation.ShouldAnimate)
            {
                StartCoroutine(Animate(animatedProj.FlyingAnimation, true));
            }
        }
        if(projectileBase is TrackingProjectile trackingProj && target != null)
        {
            StartCoroutine(Track(trackingProj, target));
        }
        var splitTimer = projectileInfo.Stages.First().split?.secondsBeforeSplit ?? int.MaxValue;
        splitTimer = splitTimer == -1 ? int.MaxValue : splitTimer;
        StartCoroutine(LifeTimer(Mathf.Min(projectileBase.lifeTime, splitTimer)));
    }

    private IEnumerator Animate(AnimatedProjectile.AnimationInfo animation, bool loop)
    {
        int CurSprite = 0;
        while (CurSprite < animation.Sprites.Length)
        {
            sr.sprite = animation.Sprites[CurSprite];
            CurSprite++;
            if(loop)
                CurSprite %= animation.Sprites.Length;
            yield return new WaitForSeconds(1f/animation.fps);
        }
        gameObject.SetActive(false);
    }

    public IEnumerator LifeTimer(float aliveSeconds)
    {
        yield return new WaitForSeconds(aliveSeconds);
        gameObject.SetActive(false);
    }

    public IEnumerator Track(TrackingProjectile proj, Transform target)
    {
        while (true)
        {
            var normDirToTarget = (target.position - transform.position).ToVector2().normalized;
            var angleDif = Vector2.SignedAngle(transform.forward.ToVector2(), normDirToTarget);

            if (Mathf.Abs(angleDif) > 1) 
            {
                rb.MoveRotation(Mathf.Sign(angleDif) * proj.angualSpeed * Time.deltaTime);
            }

            rb.AddForce(transform.forward * proj.acceleration);

            yield return null;
        }
    }
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(info.IgnoreColliders.Contains(collision))
        {
            return;
        }
        if (collision is TilemapCollider2D)
        {
            HitTarget();
        }
        else if (pierces >= 0)
        {
            info.IgnoreColliders.Add(collision);
            collision.GetComponent<HitIngress>()?.Hit(new HitData { Damage = damage, Perpetrator = Perpetrator });
            if (pierces == 0) HitTarget();
            pierces -= 1;
        }
    }

    private void HitTarget()
    {
        StopAllCoroutines();
        if (AOE > 0)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, AOE);
            foreach (var hit in hits)
            {
                var dist = Vector3.Distance(hit.transform.position, transform.position);
                hit.GetComponent<HitIngress>()?.Hit(new HitData { Damage = damage * (1 - dist / AOE), Perpetrator = Perpetrator });
            }
        }
        if (HitAnimation?.ShouldAnimate == true && info.Stages.Count <= 1)
        {
            rb.velocity = Vector2.zero;
            StartCoroutine(Animate(HitAnimation, false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (info.Stages.Count > 1)
        {
            ProjectileManager.FireStages(transform.position, rb.velocity, info, Perpetrator);
        }
        Destroy(col);
        HitAnimation = null;
        StopAllCoroutines();
    }
}
