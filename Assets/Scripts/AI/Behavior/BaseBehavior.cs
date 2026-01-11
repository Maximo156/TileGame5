using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBehavior : MonoBehaviour, IBehavior
{
    protected JobNavigator navigator;
    protected MobAnimator animator;

    [Header("Navigation Info")]
    public int Range;
    public int ViewRange;
    public float runningModifier = 1;

    [Header("General")]
    public List<ItemStack> Drops;

    protected Timer curTimer;

    protected virtual void Awake()
    {
        animator = GetComponent<MobAnimator>();
        navigator = GetComponent<JobNavigator>();
        navigator.ReachableRange = Range + 10;
    }

    public abstract Vector2Int Step(float deltaTime);

    protected void SetRandomGoal()
    {
        navigator.VectorGoal = Utilities.GetBlockPos(transform.position) + Utilities.RandomVector2Int(Range);
    }

    protected void Run()
    {
        navigator.MovementModifier = runningModifier;
    }

    protected void Walk()
    {
        navigator.MovementModifier = 1;
    }

    protected bool TargetInvalid(Vector2Int Target)
    {
        var thisToGoal = (Utilities.GetBlockPos(navigator.VectorGoal) - Utilities.GetBlockPos(transform.position.ToVector2())).magnitude;
        var goalToTarget = (Target - Utilities.GetBlockPos(navigator.VectorGoal)).magnitude;
        return goalToTarget > 0.5 * thisToGoal;
    }

    public void Despawn()
    {
        Utilities.DropItems(Utilities.GetBlockPos(transform.position), Drops);
        Destroy(gameObject);
    }

    public virtual Vector2 OverrideLastPathPos(Vector2 Target)
    {
        return Target;
    }
}
