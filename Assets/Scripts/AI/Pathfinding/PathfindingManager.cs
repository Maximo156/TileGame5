using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;
using BlockDataRepos;
using NativeRealm;

public interface IPathFinder
{
    public event Action<IPathFinder> RequestPathfinding;
    public int2 Position { get; }
    public int2 Goal { get; }
    public bool CanUseDoor { get; }
    public int ReachableRange { get; }

    /// <summary> 
    /// Sets a new path for the pathfinder
    /// </summary>
    /// <param name="stack">New path</param>
    /// <returns>True if pathfinder will dispose of path in the future: false if caller needs to dispose</returns>
    public bool SetPath(NativeStack<float2> stack, NativeList<int2> reachable);

    public bool isNull { get; }
}

public class PathfindingManager
{
    public Vector2Int curChunk { get; set; }
    RealmData worldData;
    public PathfindingManager(RealmData worldData)
    {
        this.worldData = worldData;
    }

    List<(IPathFinder ai,  NativeStack<float2> path, NativeList<int2> reachable)> activeJobs = new();
    public JobHandle RunPathfinders(IEnumerable<IPathFinder> pathfinders)
    {
        JobHandle dep = default;
        foreach (var ai in pathfinders) 
        {
            if (ai.isNull) continue;
            NativeStack<float2> path = new NativeStack<float2>(100, Allocator.Persistent);
            NativeList<int2> reachable = new NativeList<int2>(100, Allocator.Persistent);
            var job = new AstarJob()
            {
                realmData = worldData,
                chunkWidth = WorldSettings.ChunkWidth,
                blockInfo = BlockDataRepo.NativeRepo,
                canUseDoors = ai.CanUseDoor,
                End = ai.Goal,
                Start = ai.Position,
                MaxDistance = ai.ReachableRange,
                ReachableRange = ai.ReachableRange,
                Path = path,
                Reachable = reachable
            }.Schedule();
            activeJobs.Add((ai, path, reachable));
            dep = JobHandle.CombineDependencies(job, dep);
        }
        return dep;
    }

    public void ProcessPathfinders()
    {
        foreach (var (ai, path, reachable) in activeJobs)
        {
            if (!ai.SetPath(path, reachable))
            {
                path.Dispose();
            }
        }
        activeJobs.Clear();
    }
}
