using EntityStatistics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveMobBehaviour : BaseBehavior, IBehavior, IHittable
{
    public float IdleTime;
    public float PanicTime;
    public int Range;
    public int ViewRange;
    public List<Item> Edible;

    private PlayerInventories inv;

    enum PassiveMobState
    {
        Wander,
        Panic,
        Hit,
        Idle,
        Follow,
        Dead
    }

    PassiveMobState _state;
    
    PassiveMobState State
    {
        get => _state;
        set
        {
            _state = value;
            SwitchState();
            switch (_state)
            {
                case PassiveMobState.Idle:
                    curTimer = new Timer(IdleTime);
                    break;
                case PassiveMobState.Panic:
                    curTimer = new Timer(PanicTime);
                    Run();
                    break;
                case PassiveMobState.Dead:
                    Destroy(navigator);
                    return;
                case PassiveMobState.Wander:
                    break;
                case PassiveMobState.Hit:
                    animator.Play("Hit");
                    break;
            }
        }
    }

    void Awake()
    {
        State = PassiveMobState.Wander;
        inv = GameObject.FindObjectOfType<PlayerInventories>();
    }

    public override Vector2Int Step(float deltaTime)
    {
        switch (State)
        {
            case PassiveMobState.Idle:
                if (CheckInv())
                {
                    State = PassiveMobState.Follow;
                }
                else if (curTimer.Expired)
                {
                    SetRandomGoal(Range);
                    State = PassiveMobState.Wander;
                }
                break;
            case PassiveMobState.Wander:
                if (CheckInv())
                {
                    State = PassiveMobState.Follow;
                }
                else if (navigator.state == JobNavigator.State.Idle)
                {
                    State = PassiveMobState.Idle;
                }
                break;
            case PassiveMobState.Panic:
                if (curTimer.Expired)
                {
                    State = PassiveMobState.Idle;
                }
                else if (navigator.state == JobNavigator.State.Idle)
                {
                    SetRandomGoal(Range);
                }
                break;
            case PassiveMobState.Follow:
                if (!CheckInv())
                {
                    State = PassiveMobState.Idle;
                }
                else if (TargetInvalid(Utilities.GetBlockPos(inv.transform.position.ToVector2())))
                {
                    navigator.VectorGoal = Utilities.GetBlockPos(inv.transform.position.ToVector2());
                }
                break;
        }
        return default;
    }

    public void Hit()
    {
        if (State != PassiveMobState.Hit)
        {
            State = PassiveMobState.Hit;
        }
    }

    void Die()
    {
        State = PassiveMobState.Dead;
    }

    void SwitchState()
    {
        curTimer = null;
        Walk();
    }

    void ExitHit()
    {
        State = PassiveMobState.Panic;
    }

    Timer invTimer;
    bool CheckInv()
    {
        if(invTimer?.Expired == false)
        {
            return State == PassiveMobState.Follow;
        }
        invTimer = new Timer(0.3f);
        var diff = inv.transform.position - transform.position;

        return diff.magnitude < ViewRange &&
               diff.magnitude > 1 &&
               inv.curInHandItem != null &&
               Edible.Count > 0 &&
               Edible.Contains(inv.curInHandItem) &&
               Physics2D.Raycast(transform.position, diff.normalized, ViewRange, LayerMask.GetMask("Terrain")).collider == null;
    }
}
