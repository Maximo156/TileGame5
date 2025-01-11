using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Projectiles/AnimatedProjectile", order = 1)]
public class AnimatedProjectile : Projectile
{
    [Serializable]
    public class AnimationInfo
    {
        public Sprite[] Sprites;
        public float fps;

        public bool ShouldAnimate => Sprites.Length > 0;
    }

    [Header("Projectile animations")]
    public AnimationInfo FlyingAnimation;
    public AnimationInfo HitAnimation;
}
