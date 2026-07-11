using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using NativeRealm;

public class PathProvider
{
    const int MaxDistance = 25;

    Dictionary<uint, Request> OpenRequests = new Dictionary<uint, Request>();
    Dictionary<uint, Result> Results = new Dictionary<uint, Result>();

    RealmData RealmData;
    int chunkWidth;

    uint nextId = 0;

    public PathProvider(RealmData data, int chunkWidth)
    {
        RealmData = data;
        this.chunkWidth = chunkWidth;
    }

    public uint RequestPath(Request req)
    {
        var id = ++nextId;
        OpenRequests[id] = req;
        return id;
    }

    public void DropRequest(uint id)
    {
        if (Results.Remove(id, out var res))
        {
            res.Job.Complete();
            res.Path.Dispose();
        }
    }

    public bool TryGetResult(uint id, out NativeStack<float2> path)
    {
        if (!Results.ContainsKey(id) && !OpenRequests.ContainsKey(id)) throw new System.Exception($"Unknown path request: {id}");
        if(Results.TryGetValue(id, out var res) && res.Job.IsCompleted)
        {
            res.Job.Complete();
            path = res.Path;
            Results.Remove(id);
            return true;
        }
        path = default;
        return false;
    }

    public JobHandle SchedulePathfinding(JobHandle dep = default)
    {
        JobHandle combinedDep = default;
        foreach(var kvp in OpenRequests)
        {
            var id = kvp.Key;
            var request = kvp.Value;
            var res = new NativeStack<float2>(0, Allocator.Persistent);
            var reachable = new NativeList<int2>(0, Allocator.Persistent);

            var searchBuffer = new SearchBuffer(MaxDistance, Allocator.TempJob);
            var OpenSet = new NativeBinaryMinHeap((2 * MaxDistance + 1) * (2 * MaxDistance + 1) / 2, Allocator.TempJob);

            var handle = new AStarOptimizedJob()
            {
                Start = request.start,
                End = request.dest,
                canUseDoors = request.canUseDoors,
                MaxDistance = MaxDistance,
                ReachableRange = 25,
                Path = res,
                realmData = RealmData,
                Reachable = reachable,
                blockInfo = BlockDataRepos.BlockDataRepo.NativeRepo,
                chunkWidth = chunkWidth,
                Open = OpenSet,
                Search = searchBuffer
            }.Schedule(dep);

            handle = JobHandle.CombineDependencies(reachable.Dispose(handle), OpenSet.Dispose(handle), searchBuffer.Dispose(handle));
            combinedDep = JobHandle.CombineDependencies(combinedDep, handle);
            Results.Add(id, new Result() { Job = handle, Path = res });
        }
        OpenRequests.Clear();
        return combinedDep;
    }

    public struct Request
    {
        public int2 start;
        public int2 dest;
        public bool canUseDoors;

        // Pass additional per agent params
    }

    struct Result
    {
        public NativeStack<float2> Path;
        public JobHandle Job;
    }
}
