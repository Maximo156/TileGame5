using UnityEngine;

[CreateAssetMenu(fileName = "NewTargetingCharm", menuName = "Inventory/Charms/TargetingCharm", order = 1)]
public class TargetingCharm : ItemCharm
{
    [Stat("TargetingStrength")]
    public float targetingForce;

    [Stat("TargetingRange")]
    public float TargetingRange;

    public override bool IsExclusive => true;
}
