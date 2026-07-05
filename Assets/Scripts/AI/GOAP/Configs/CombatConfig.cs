using UnityEngine;

[CreateAssetMenu(fileName = "CombatConfig", menuName = "AI/CombatConfig")]
public class CombatConfig : ScriptableObject
{
    public float AttackRange;
    public float viewRange;
    public float BaseDamage;

    public LayerMask mask;
}
