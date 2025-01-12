using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Projectiles/BasicProjectile", order = 1)]
public class Projectile : ScriptableObject, ISpriteful
{
    [Header("Stats")]
    [Stat("Damage")]
    public int damage;
    [Stat("Speed")]
    public float speed;
    [Stat("Range")]
    public float range;
    public float scale;
    [Stat("Peirce", "Infinite")]
    public float peirce;
    public float AOE = 0;
    public float lifeTime => range / speed;

    [Header("Display")]
    public Sprite sprite;
    public Color color = Color.white;
    public Sprite Sprite => sprite;
}
