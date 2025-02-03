using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System;

public class JobNavigator : MonoBehaviour, IPathFinder
{
    public event Action<IPathFinder> RequestPathfinding = delegate { };
    public event Action<IPathFinder> RecievedPath = delegate { };

    public bool CanUseDoors;
    public float MovementSpeed;

    public Animator animator;
    public SpriteRenderer sprite;

    NativeStack<int2> _path;
    int2 _goal;
    NativeList<int2> _reachable;
    Action<JobNavigator> onStuck;

    public enum State
    {
        Navigating,
        Idle,
        Moving,
        Stuck
    }

    public bool CanUseDoor => CanUseDoors;
    public int ReachableRange { get; set; } = 20;
    public float MovementModifier { get; set; } = 1;
    public bool isNull => this == null;

    State _state = State.Idle;
    public State state
    {
        get => _state;
        private set
        {
            _state = value;
            switch (_state)
            {
                case State.Idle:
                    SetMovementInfo(default);
                    break;
            }
            TriggerPathFinding();
        }
    }
    public int2 Position
    {
        get
        {
            var block = Utilities.GetBlockPos(transform.position);
            return new int2(block.x, block.y);
        }
    }
    public int2 Goal
    {
        get => _goal;
        set
        {
            Path = default;
            state = State.Navigating;
            _goal = value;
        }
    }
    NativeStack<int2> Path
    {
        get => _path;
        set
        {
            if (_path.IsCreated)
            {
                _path.Dispose();
            }
            _path = value;
            TriggerPathFinding();
        }
    }
    NativeList<int2> Reachable
    {
        get => _reachable;
        set
        {
            if (_reachable.IsCreated)
            {
                _reachable.Dispose();
            }
            _reachable = value;
        }
    }

    void TriggerPathFinding()
    {
        if(!Path.IsCreated && state != State.Stuck)
        {
            RequestPathfinding(this);
        }
    }

    private void Awake()
    {
        _path = new NativeStack<int2>(1, Allocator.Persistent);
        onStuck = SelectAndSetRandomGoal;
    } 

    public bool SetPath(NativeStack<int2> stack, NativeList<int2> reachable)
    {
        Reachable = reachable;
        if (stack.Count == 0)
        {
            Path = default;
            state = State.Stuck;
            onStuck(this);
            RecievedPath(this);
            return false;
        }
        state = State.Moving;
        Path = stack;
        RecievedPath(this);
        return true;
    }

    public bool TryGetReachable(out Vector2Int outPos)
    {
        if (Reachable.Length > 0)
        {
            var pos = Reachable[UnityEngine.Random.Range(0, Reachable.Length)];
            outPos = new Vector2Int(pos.x, pos.y);
            return true;
        }
        outPos = default;
        return false;
    }

    BlockSlice Target;
    BlockSlice Current;
    Vector2Int CurrentPos;

    public void Move(float deltaTime)
    {
        if (state != State.Moving) return;
        if (!Path.IsCreated || Path.Count == 0) {
            state = State.Idle;
            Target = null;
            return;
        }
        var nextInt = Path.Peek();
        var next = new Vector2Int(nextInt.x, nextInt.y);
        if(Target is null)
        {
            ChunkManager.TryGetBlock(next, out Target);
        }
        if(Target is null || !Target.Walkable || (Target.WallBlock is Door && !CanUseDoors))
        {
            state = State.Stuck;
            Target = null;
            return;
        }
        var curBlock = Utilities.GetBlockPos(transform.position);
        if(curBlock != CurrentPos)
        {
            CurrentPos = curBlock;
            ChunkManager.TryGetBlock(CurrentPos, out Current);
        }

        var difference = (Utilities.GetBlockCenter(next).ToVector3() - transform.position);
        var dir = difference.normalized;
        var movement = ((Current?.MovementSpeed ?? 1) * deltaTime * MovementModifier * MovementSpeed * dir);
        SetMovementInfo(dir);
        transform.position = transform.position + movement;

        if (difference.magnitude < 0.2)
        {
            Path.Pop();
            Target = null;
        }
    }

    private void OnDestroy()
    {
        if (Path.IsCreated)
        {
            Path = default;
        }
        if (Reachable.IsCreated)
        {
            Reachable = default;
        }
    }

    public Vector2Int VectorGoal
    {
        get
        {
            return new Vector2Int(Goal.x, Goal.y);
        }
        set
        {
            var tmp = Vector2Int.FloorToInt(value);
            Goal = new int2(tmp.x, tmp.y);
        }
    }

    public void SetStuckBehavior(Action<JobNavigator> onStuck)
    {
        this.onStuck = onStuck ?? SelectAndSetRandomGoal;
    }

    void SelectAndSetRandomGoal(JobNavigator _)
    {
        if(TryGetReachable(out var newPos))
        {
            Goal = new int2(newPos.x, newPos.y);
        }
    }

    void SetMovementInfo(Vector2 LatestDir)
    {
        animator.SetFloat("Speed", LatestDir.magnitude);
        sprite.flipX = LatestDir.x < 0 || (LatestDir.magnitude == 0 && sprite.flipX);
    }
}
