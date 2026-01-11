using EntityStatistics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyMobBehaviour : BaseBehavior, IBehavior, IHittable
{
    public float AttackRange;
    public float IdleTime;

    public float AttackDamage;

    private HitIngress playerHit;
    private Transform player => playerHit.transform;

    public enum EnemyMobState
    {
        Wander,
        Hit,
        Attack,
        Idle,
        Chase,
        Dead
    }

    bool lockState;

    public EnemyMobState _state;

    EnemyMobState State
    {
        get => _state;
        set
        {
            if(!CanChangeState(value))
            {
                return;
            }
            _state = value;
            lockState = false;
            SwitchState();
            switch (_state)
            {
                case EnemyMobState.Chase:
                    Run();
                    break;
                case EnemyMobState.Idle:
                    curTimer = new Timer(IdleTime);
                    break;
                case EnemyMobState.Dead:
                    lockState = true;
                    animator.PlayAnimation("Die", Despawn);
                    Destroy(navigator);
                    break;
                case EnemyMobState.Hit:
                    lockState = true;
                    animator.PlayAnimation("Hit", ExitHit);
                    break;
                case EnemyMobState.Attack:
                    lockState = true; 
                    animator.PlayAnimation("Attack", ExitAttack);
                    break;
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        State = EnemyMobState.Wander;
        playerHit = Camera.main.transform.parent.GetComponent<HitIngress>();
    }

    public override Vector2Int Step(float deltaTime)
    {
        if (lockState)
        {
            return default;
        }
        if (distanceToTarget < AttackRange)
        {
            State = EnemyMobState.Attack;
            return default;
        }
        switch (State)
        {
            case EnemyMobState.Idle:
                if (FindTarget())
                {
                    State = EnemyMobState.Chase;
                }
                else if (curTimer.Expired)
                {
                    SetRandomGoal();
                    State = EnemyMobState.Wander;
                }
                break;
            case EnemyMobState.Wander:
                if (FindTarget())
                {
                    State = EnemyMobState.Chase;
                }
                else if (navigator.state == JobNavigator.State.Idle)
                {
                    State = EnemyMobState.Idle;
                }
                break;
            case EnemyMobState.Chase:
                if (!FindTarget())
                {
                    State = EnemyMobState.Idle;
                }
                else if (TargetInvalid(Utilities.GetBlockPos(player.position.ToVector2())))
                {
                    navigator.VectorGoal = Utilities.GetBlockPos(player.position.ToVector2());
                }
                break;
        }
        return default;
    }

    private bool CanChangeState(EnemyMobState newState)
    {
        if (State != EnemyMobState.Dead && newState == EnemyMobState.Dead)
        {
            return true;
        }
        if (State == EnemyMobState.Hit && newState != EnemyMobState.Idle)
        {
            return false;
        }
        if(State == EnemyMobState.Dead)
        {
            return false;
        }
        return true;
    }

    public void Hit(HitData _)
    {
        if (State != EnemyMobState.Hit && State != EnemyMobState.Dead)
        {
            State = EnemyMobState.Hit;
        }
    }

    void Die()
    {
        if (State != EnemyMobState.Dead)
        {
            State = EnemyMobState.Dead;
        }
    }

    void SwitchState()
    {
        curTimer = null;
        Walk();
    }

    void ExitHit()
    {
        State = EnemyMobState.Idle;
    }

    void ExitAttack()
    {
        State = EnemyMobState.Chase;
    }

    Timer invTimer;
    bool FindTarget()
    {
        if(invTimer?.Expired == false)
        {
            return State == EnemyMobState.Chase;
        }
        invTimer = new Timer(0.3f);
        var diff = difToTarget;
        var dist = distanceToTarget;
        var res = dist < ViewRange &&
               dist > AttackRange * 0.75 &&
               Physics2D.Raycast(transform.position, diff.normalized, distanceToTarget, LayerMask.GetMask("Terrain")).collider == null;
        return res;
    }

    protected void AttackCallBack()
    {
        if(distanceToTarget < AttackRange * 1.1)
        {
            playerHit.Hit(new HitData()
            {
                Perpetrator = this.transform,
                Damage = AttackDamage
            });
        }
    }

    protected Vector3 difToTarget => player.position - transform.position;
    protected float distanceToTarget => difToTarget.magnitude;

    public override Vector2 OverrideLastPathPos(Vector2 Target)
    {
        return player.position;
    }
}
