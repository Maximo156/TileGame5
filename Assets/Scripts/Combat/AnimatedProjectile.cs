using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Projectiles/AnimatedProjectile", order = 1)]
public class AnimatedProjectile : Projectile
{
    [Header("Animation")]
    public List<Sprite> Sprites;
    public int fps;
}
