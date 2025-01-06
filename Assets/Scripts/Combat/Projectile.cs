using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Projectiles/BasicProjectile", order = 1)]
public class Projectile : ScriptableObject
{
    [Header("Stats")]
    public int damage;
    public float speed;
    public float range;
    public float scale;
    public float peirce;

    public float lifeTime => range / speed;

    [Header("Display")]
    public Sprite sprite;
    public Color color = Color.white;
}
