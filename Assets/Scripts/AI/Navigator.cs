using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AStarSharp;
using System;

public class Navigator : MonoBehaviour
{
    public bool CanUseDoors;
    public float MovementSpeed;

    public enum State
    {
        Navigating,
        Idle
    }

    public State state { get; private set; } = State.Idle;

    Vector2Int _goal;
    public Vector2Int Goal 
    {
        get => _goal;
        set {
            if (value == _goal) return;
            LatestResult = null;
            _goal = value;
            if (CancellationToken is not null)
            {
                CancellationToken.Cancel();
            }
            if(_goal != null)
            {
                StartPathFinding();
            }
        }
    }

    CancellationTokenSource CancellationToken;
    public Action<PathFindingResult> onFailedPathing;
    PathFindingResult LatestResult;

    void StartPathFinding()
    {
        if (CancellationToken is not null)
        {
            throw new InvalidOperationException("Starting new pathfinding before completing old");
        }
        LatestResult = null;
        state = State.Navigating;
        CancellationToken = new CancellationTokenSource();
        RunPathFinding(Utilities.GetBlockPos(transform.position), Goal);
    }

    void RunPathFinding(Vector2Int Start, Vector2Int End)
    {
        Task.Run(() =>
        {
            try
            {
                var res = Astar.FindPathNonCo(Start, End, Astar.GetAdjacentNodes, CanUseDoors, 500, CancellationToken.Token);
                CancellationToken = null;
                if (!res.FoundGoal && !res.canceled)
                {
                    state = State.Idle;
                    CallbackManager.AddCallback(() => onFailedPathing?.Invoke(res));
                    return;
                }
                LatestResult = res;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                state = State.Idle;
                CancellationToken = null;
            }
        });
    }

    BlockSlice Target;
    public void Move(float deltaTime)
    {
        if (CancellationToken is not null) return;
        if (LatestResult is null || LatestResult.path is null || LatestResult.path.Count == 0) {
            state = State.Idle;
            LatestResult = null;
            return;
        }
        var next = LatestResult.path.Peek();
        if(Target is null)
        {
            ChunkManager.TryGetBlock(next, out Target);
        }
        if(Target is null || !Target.Walkable || (Target.WallBlock is Door && !CanUseDoors))
        {
            StartPathFinding();
            return;
        }

        var difference = (Utilities.GetBlockCenter(next).ToVector3() - transform.position);
        var dir = difference.normalized;

        transform.position = transform.position + (Target.MovementSpeed * MovementSpeed * dir * deltaTime);

        if (difference.magnitude < 0.2)
        {
            LatestResult.path.Pop();
        }
    }

    private void OnDisable()
    {
        if (CancellationToken is not null)
        {
            CancellationToken.Cancel();
        }
    }
}
