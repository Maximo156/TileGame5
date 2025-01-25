using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class JobNavigator : MonoBehaviour, IPathFinder
{
    public int m_ReachableRange;
    public bool CanUseDoors;
    public float MovementSpeed;

    public int2 Position
    {
        get
        {
            var block = Utilities.GetBlockPos(transform.position);
            return new int2(block.x, block.y);
        }
    }
    int2 _goal;
    public int2 Goal { 
        get => _goal; 
        set 
        {
            Path = default;
            state = State.Navigating;
            _goal = value;
        } 
    }
    public bool NeedPath => !Path.IsCreated && state != State.Stuck;
    public bool CanUseDoor => CanUseDoors;
    public int ReachableRange => m_ReachableRange;

    public enum State
    {
        Navigating,
        Idle,
        Moving,
        Stuck
    }

    public State state { get; private set; } = State.Idle;

    NativeStack<int2> _path;
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
        }
    }

    NativeList<int2> _reachable;
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

    private void Awake()
    {
        _path = new NativeStack<int2>(1, Allocator.Persistent);
    } 

    public bool SetPath(NativeStack<int2> stack, NativeList<int2> reachable)
    {
        Reachable = reachable;
        if (stack.Count == 0)
        {
            Path = default;
            state = State.Stuck;
            return false;
        }
        state = State.Moving;
        Path = stack;
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

        transform.position = transform.position + ((Current?.MovementSpeed ?? 1) * MovementSpeed * dir * deltaTime);

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

    public Vector2 VectorGoal
    {
        get
        {
            return new Vector2(Goal.x, Goal.y);
        }
        set
        {
            var tmp = Vector2Int.FloorToInt(value);
            Goal = new int2(tmp.x, tmp.y);
        }
    }
}
