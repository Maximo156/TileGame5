using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockBack : MonoBehaviour, IHittable
{
    public float force;

    Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Hit(HitData info)
    {
        var dif = transform.position - info.Perpetrator.position;
        rb.AddForce(force * dif.normalized, ForceMode2D.Impulse);
        print(dif);
    }
}
