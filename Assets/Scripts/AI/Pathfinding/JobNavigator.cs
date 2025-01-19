using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AStarSharp;
using System;
using Unity.Collections;
using Unity.Mathematics;

public class JobNavigator : MonoBehaviour, IPathFinder
{
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
    public int2 Goal { get; set; }
    public bool NeedPath => !_path.IsCreated || Path.Count == 0 || state == JobNavigator.State.Idle;

    public bool CanUseDoor => CanUseDoors;

    public enum State
    {
        Navigating,
        Idle
    }

    public State state { get; private set; } = State.Idle;

    NativeStack<int2> _path = default;
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

    public bool SetPath(NativeStack<int2> stack)
    {
        if(stack.Count == 0)
        {
            Path = default;
            return false;
        }
        Path = stack;
        return true;
    }

    BlockSlice Target;
    BlockSlice Current;
    Vector2Int CurrentPos;
    public void Move(float deltaTime)
    {
        if (!Path.IsCreated || Path.Count == 0) {
            state = State.Idle;
            Path = default;
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
            state = State.Idle;
            Path = default;
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
    }
}
