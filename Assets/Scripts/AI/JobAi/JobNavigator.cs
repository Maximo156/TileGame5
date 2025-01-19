using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AStarSharp;
using System;
using Unity.Collections;
using Unity.Mathematics;

public class JobNavigator : MonoBehaviour
{
    public bool CanUseDoors;
    public float MovementSpeed;
    public enum State
    {
        Navigating,
        Idle
    }
    public State state { get; private set; } = State.Idle;
    public bool NeedPath => !_path.IsCreated || Path.Count == 0;

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
    public void Move(float deltaTime)
    {
        if (!Path.IsCreated || Path.Count == 0) {
            state = State.Idle;
            Path = default;
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
            return;
        }

        var difference = (Utilities.GetBlockCenter(next).ToVector3() - transform.position);
        var dir = difference.normalized;

        transform.position = transform.position + (Target.MovementSpeed * MovementSpeed * dir * deltaTime);

        if (difference.magnitude < 0.2)
        {
            Path.Pop();
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
