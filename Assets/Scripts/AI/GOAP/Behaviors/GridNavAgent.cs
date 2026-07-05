using CrashKonijn.Agent.Core;
using System;
using System.Linq;
using System.Runtime;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class GridNavAgent : MonoBehaviour
{
    public event Action<Vector2> OnLocomotionUpdate;
    public event Action OnStuck;

    public float directMoveDist = 1;
    public float BaseMoveSpeed = 1;
    public float runMultiplier = 1.5f;
    public bool canOpenDoors = false;

    public LayerMask terrainMask;

    public ITarget Target { get; private set; }

    bool shouldMove = true;

    bool running;

    float moveSpeed => BaseMoveSpeed * (running ? runMultiplier : 1);

    Vector2 destPosition;
    NativeStack<float2> CurrentPath;
    bool pathContinues => CurrentPath.IsCreated && CurrentPath.Count > 0;

    public Vector2 TargetDifference => Target.IsValid() ? Target.Position - transform.position : default;

    private void Update()
    {
        if (Target != null && shouldMove && TargetDifference.magnitude > 0.1f)
        {
            PerformMoveUpdate();
        }
        else
        {
            OnLocomotionUpdate?.Invoke(default);
        }
    }

    void PerformMoveUpdate()
    { 
        var canSeeTarget = CanDirectPath();
        CheckAndRequestPath(canSeeTarget);
        ResolvePath();

        var moveDir = (canSeeTarget ? TargetDifference : PathDirection()) + ColliderPush();

        OnLocomotionUpdate?.Invoke(moveDir);

        var movement = Time.deltaTime * moveSpeed * moveDir.normalized.ToVector3();
        transform.position += movement;
    }

    public bool CanDirectPath()
    {
        return Vector3.Distance(transform.position, Target.Position) < directMoveDist &&
            !Physics2D.CircleCast(transform.position, 1, TargetDifference, TargetDifference.magnitude, terrainMask);
    }

    public Vector2 PathDirection()
    {
        if (!CurrentPath.IsCreated || CurrentPath.Count == 0)
        {
            return default;
        }
        var t = CurrentPath.Peek();

        return new Vector2(t.x, t.y) - transform.position.ToVector2();
    }

    public Vector2 ColliderPush()
    {
        return default;
    }

    uint? requestedPath;
    public void ResolvePath()
    {
        if (!requestedPath.HasValue) return;
        
        if(ChunkManager.TryGetPathResult(requestedPath.Value, out var newPath))
        {
            if (CurrentPath.IsCreated) CurrentPath.Dispose();
            CurrentPath = newPath;
            requestedPath = null;
            if(CurrentPath.Count == 0)
            {
                OnStuck?.Invoke();
            }
        }
    }

    float lastRequestTime;
    public void CheckAndRequestPath(bool canSeeTarget)
    {
        TryInvalidatePath();
        if(ShouldRequestNewPath(canSeeTarget))
        {
            RequestNewPath();
        }
        if (!pathContinues) return;
        if(math.distance(CurrentPath.Peek(), transform.position.ToVector2()) < 0.25f)
        {
            CurrentPath.Pop();
        }
    }

    bool ShouldRequestNewPath(bool canSeeTarget)
    {
        return (TargetDifference.magnitude > directMoveDist || !canSeeTarget) &&
            !requestedPath.HasValue &&
            Time.time - lastRequestTime > 0.2 &&
            (!CurrentPath.IsCreated || CurrentPath.Count == 0 || Vector3.Distance(Target.Position, destPosition) > 1);
    }

    void RequestNewPath()
    {
        if (requestedPath.HasValue) throw new Exception("Path already requested");
        var targetBlock = Utilities.GetBlockPos(Target.Position);
        destPosition = Utilities.GetBlockCenter(targetBlock);

        requestedPath = ChunkManager.RequestPath(
            new()
            {
                start = Utilities.GetBlockPos(transform.position).ToInt(),
                dest = targetBlock.ToInt(),
                canUseDoors = canOpenDoors
            });
        lastRequestTime = Time.time;    
    }

    void TryInvalidatePath()
    {
        if (!pathContinues || !ChunkManager.TryGetBlock(Utilities.GetBlockPos(CurrentPath.Peek()), out var slice)) return;
        var moveInfo = slice.GetMovementInfo();
        if (moveInfo.walkable || (moveInfo.door && canOpenDoors)) return;
        CurrentPath.Dispose();
        CurrentPath = default;
    }

    public void SetTarget(ITarget dest)
    {
        Target = dest;
    }

    public void Pause()
    {
        shouldMove = false;
    }

    public void Resume()
    {
        shouldMove = true;
    }

    public void Run()
    {
        running = true;
    }

    public void StopRunning()
    {
        running = false;
    }

    private void OnDisable()
    {
        if (CurrentPath.IsCreated) CurrentPath.Dispose();
        if (requestedPath.HasValue) ChunkManager.DropPathRequest(requestedPath.Value);
        requestedPath = null;
    }

    private void OnDrawGizmos()
    {
        if (CurrentPath.IsCreated)
        {
            foreach (var p in CurrentPath)
            {
                Gizmos.DrawWireCube(new Vector3(p.x, p.y, 0), Vector3.one);
            }
        }
    }
}
