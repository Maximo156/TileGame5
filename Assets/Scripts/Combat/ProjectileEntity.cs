using System.Collections;
using System.Collections.Generic;
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

    void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        sr = gameObject.AddComponent<SpriteRenderer>();
        gameObject.layer = LayerMask.NameToLayer("Projectile");
    }

    public void Setup(Projectile projectileBase, Vector2 position, Vector2 dir, ProjectileInfo projectileInfo, int renderLayer, Transform perpetrator, Transform target = null)
    {
        transform.localScale = projectileBase.scale * projectileInfo.scaleMultiplier * Vector3.one;
        transform.position = position;
        transform.right = dir;
        rb.velocity = projectileBase.speed * projectileInfo.speedMultiplier * dir.normalized;
        damage = projectileBase.damage * projectileInfo.damageMultiplier;
        sr.sprite = projectileBase.sprite;
        sr.color = projectileBase.color;
        sr.sortingOrder = renderLayer;
        col = gameObject.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;
        Perpetrator = perpetrator;
        AOE = projectileBase.AOE;
        pierces = projectileBase.peirce;
        Physics2D.IgnoreCollision(col, projectileInfo.UserCollider);
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
        StartCoroutine(LifeTimer(projectileBase.lifeTime));
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
        if (collision is TilemapCollider2D)
        {
            HitTarget();
        }
        else if (pierces >= 0)
        {
            collision.SendMessage("Hit", new HitData { Damage = damage, Perpetrator = Perpetrator }, SendMessageOptions.DontRequireReceiver);
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
                hit.attachedRigidbody?.SendMessage("Hit", new HitData { Damage = damage * (1 - dist / AOE), Perpetrator = Perpetrator }, SendMessageOptions.DontRequireReceiver);
            }
        }
        if (HitAnimation?.ShouldAnimate == true)
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
        Destroy(col);
        HitAnimation = null;
        StopAllCoroutines();
    }
}
