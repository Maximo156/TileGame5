using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;

public interface IPathFinder
{
    public int2 Position { get; }
    public int2 Goal { get; }
    public bool CanUseDoor { get; }
    public bool NeedPath { get; }

    public int ReachableRange { get; }

    /// <summary>
    /// Sets a new path for the pathfinder
    /// </summary>
    /// <param name="stack">New path</param>
    /// <returns>True if pathfinder will dispose of path in the future: false if caller needs to dispose</returns>
    public bool SetPath(NativeStack<int2> stack, NativeList<int2> reachable);
}

public class PathfindingManager : IDisposable
{
    NativeHashMap<int2, BlockSliceData> blockDataMirror;
    public Vector2Int curChunk { get; set; }

    public PathfindingManager()
    {
        blockDataMirror = new NativeHashMap<int2, BlockSliceData>(1024, Allocator.Persistent);
    }

    Queue<(int2 pos, BlockSliceData data)> blockChanges = new();
    public void OnBlockChanged(Vector2Int worldPos, BlockSlice block)
    {
        blockChanges.Enqueue((new int2(worldPos.x, worldPos.y), block.GetData()));
    }

    Queue<Chunk> ChunkChanges = new();
    public void OnChunkChanged(Chunk chunk)
    {
        ChunkChanges.Enqueue(chunk);
    }

    HashSet<(IPathFinder ai, JobHandle handle, NativeStack<int2> path, NativeList<int2> reachable)> activeJobs;
    public IEnumerator RunPathfinders(IEnumerable<IPathFinder> pathfinders)
    {
        while (ChunkChanges.TryDequeue(out var c))
        {
            if (c != null) c.UpdateBlockData(ref blockDataMirror);
        }
        while (blockChanges.TryDequeue(out var info))
        {
            blockDataMirror[info.pos] = info.data;
        };
        yield return null;
        activeJobs = new HashSet<(IPathFinder ai, JobHandle handle, NativeStack<int2> path, NativeList<int2> reachable)> (pathfinders.Where(ai => ai is not null && ai.NeedPath).Select(pathFinder =>
        {
            NativeStack<int2> path = new NativeStack<int2>(100, Allocator.Persistent);
            NativeList<int2> reachable = new NativeList<int2>(100, Allocator.Persistent);
            var job = new AstarJob()
            {
                BlockData = blockDataMirror,
                canUseDoors = pathFinder.CanUseDoor,
                End = pathFinder.Goal,
                Start = pathFinder.Position,
                MaxDistance = 300,
                ReachableRange = pathFinder.ReachableRange,
                Path = path,
                Reachable = reachable
            };
            return ( pathFinder, handle: job.Schedule(), path: path, reachable: reachable);
        }));
        yield return null;
        while (activeJobs.Count > 0)
        {
            foreach (var info in activeJobs.ToList().Where(j => j.handle.IsCompleted))
            {
                info.handle.Complete();
                if (!info.ai.SetPath(info.path, info.reachable))
                {
                    info.path.Dispose();
                }
                activeJobs.Remove(info);
            }
            yield return null;
        }
    }

    public void Dispose()
    {
        if (activeJobs != null)
        {
            foreach (var job in activeJobs)
            {
                job.handle.Complete();
                job.path.Dispose();
                job.reachable.Dispose();
            }
        }
        blockDataMirror.Dispose();
    }
}
