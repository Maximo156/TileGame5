using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Projectiles/TrackingProjectile", order = 1)]
public class TrackingProjectile : AnimatedProjectile
{
    [Header("Tracking")]
    public float acceleration;
    public float angualSpeed;
}
