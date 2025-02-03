using UnityEngine;

public abstract class BaseBehavior : MonoBehaviour, IBehavior
{

    public JobNavigator navigator;
    public Animator animator;
    public int Range;
    public int ViewRange;
    public float runningModifier = 1;

    protected Timer curTimer;

    private void Awake()
    {
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
}
