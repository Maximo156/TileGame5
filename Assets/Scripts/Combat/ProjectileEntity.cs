using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileEntity : MonoBehaviour
{
    Rigidbody2D rb;
    BoxCollider2D col;
    SpriteRenderer sr;

    float damage;

    void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        col = gameObject.AddComponent<BoxCollider2D>();
        sr = gameObject.AddComponent<SpriteRenderer>();
    }

    public void Setup(Projectile projectileBase, Vector2 position, Vector2 dir, ProjectileModifier modifier, Transform target = null)
    {
        transform.localScale = projectileBase.scale * modifier.scaleMultiplier * Vector3.one;
        transform.position = position;
        transform.right = dir;
        rb.velocity = projectileBase.speed * modifier.speedMultiplier * dir.normalized;
        damage = projectileBase.damage * modifier.damageMultiplier;
        sr.sprite = projectileBase.sprite;
        sr.color = projectileBase.color;

        if(projectileBase is AnimatedProjectile animatedProj && animatedProj.Sprites.Count > 0)
        {
            StartCoroutine(Animate(animatedProj));
        }
        if(projectileBase is TrackingProjectile trackingProj && target != null)
        {
            StartCoroutine(Track(trackingProj, target));
        }
        StartCoroutine(LifeTimer(projectileBase.lifeTime));
    }

    public IEnumerator Animate(AnimatedProjectile proj)
    {
        int curSprite = 0;
        while (true)
        {
            sr.sprite = proj.Sprites[curSprite];
            curSprite = (curSprite + 1) % proj.Sprites.Count;
            yield return new WaitForSeconds(1f/proj.fps);
        }
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
