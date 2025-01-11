using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Projectiles/BasicProjectile", order = 1)]
public class Projectile : ScriptableObject, ISpriteful
{
    [Header("Stats")]
    public int damage;
    public float speed;
    public float range;
    public float scale;
    public float peirce;
    public float AOE = 0;
    public float lifeTime => range / speed;

    [Header("Display")]
    public Sprite sprite;
    public Color color = Color.white;
    public Sprite Sprite => sprite;
}
