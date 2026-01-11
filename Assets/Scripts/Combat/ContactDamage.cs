using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    public float contactDamage;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody.tag == "Tool") return;
        collision.GetComponentInParent<HitIngress>()?.Hit(new HitData()
        {
            Perpetrator = transform,
            Damage = contactDamage
        });
    }
}
