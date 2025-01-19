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

    /// <summary>
    /// Sets a new path for the pathfinder
    /// </summary>
    /// <param name="stack">New path</param>
    /// <returns>True if pathfinder will dispose of path in the future: false if caller needs to dispose</returns>
    public bool SetPath(NativeStack<int2> stack);
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

    public IEnumerator RunChunk(IEnumerable<IPathFinder> pathfinders)
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
        var jobs = pathfinders.Where(ai => ai is not null && ai.NeedPath).Select(ai =>
        {
            var pathFinder = ai as IPathFinder;
            NativeStack<int2> path = new NativeStack<int2>(100, Allocator.Persistent);
            var job = new AstarJob()
            {
                BlockData = blockDataMirror,
                canUseDoors = pathFinder.CanUseDoor,
                End = pathFinder.Goal,
                Start = pathFinder.Position,
                MaxDistance = 300,
                Path = path
            };
            return (pathFinder, job.Schedule(), path);
        });
        yield return null;
        foreach (var (ai, job, path) in jobs)
        {
            job.Complete();
            if (!ai.SetPath(path))
            {
                path.Dispose();
            }
        }
    }

    public void Dispose()
    {
        blockDataMirror.Dispose();
    }
}
