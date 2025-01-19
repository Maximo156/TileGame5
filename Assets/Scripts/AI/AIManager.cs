using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

public interface IStepable
{
    public Vector2Int Step(float deltaTime);

    public Transform Transform { get; }
}

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

public class AIManager : MonoBehaviour
{
    public int AiSimDistance = 10;
    Dictionary<Vector2Int, Chunk> LoadedChunks;
    NativeHashMap<int2, BlockSliceData> blockDataMirror;

    int ChunkWidth;
    public Vector2Int curChunk { get; set; }
    Queue<Vector2Int> SimQueue;

    HashSet<IStepable> UnParentedAi = new();
    public void Initialize(Dictionary<Vector2Int, Chunk> LoadedChunks, int ChunkWidth)
    {
        blockDataMirror = new NativeHashMap<int2, BlockSliceData>(1024, Allocator.Persistent);
        this.LoadedChunks = LoadedChunks;
        this.ChunkWidth = ChunkWidth;
    }

    public void CleanUp()
    {
        blockDataMirror.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        foreach(var ai in UnParentedAi.ToList())
        {
            RegisterImpl(ai);
        }
        if (SimQueue == null || SimQueue.Count == 0)
        {
            SimQueue = new Queue<Vector2Int>(Utilities.Spiral(curChunk, (uint)AiSimDistance));
        }
        if(!running && SimQueue.TryDequeue(out var chunkPos) && LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if (chunk.ais.Count > 0)
            {
                print($"{chunkPos}: {chunk.ais.Count} ais running");
            }
            StartCoroutine(RunChunk(chunk));
        }
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

    bool running;
    public IEnumerator RunChunk(Chunk chunk)
    {
        running = true;

        while(ChunkChanges.TryDequeue(out var c))
        {
            if(c!= null) c.UpdateBlockData(ref blockDataMirror);
        }
        while(blockChanges.TryDequeue(out var info))
        {
            blockDataMirror[info.pos] = info.data;
        };

        yield return null;

        var jobs = chunk.ais.Where(ai => ai is IPathFinder pathFinder && pathFinder.NeedPath).Select(ai =>
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

        foreach (var ai in chunk.ais.ToList())
        {
            ai.Step(Time.deltaTime);            
            var newChunkPos = Utilities.GetChunk(Utilities.GetBlockPos(ai.Transform.position), ChunkWidth);
            if (newChunkPos != chunk.ChunkPos && LoadedChunks.TryGetValue(newChunkPos, out var newChunk))
            {
                chunk.ais.Remove(ai);
                newChunk.AddChild(ai);
            }
            yield return null;
        }
        running = false;
    }

    private void RegisterImpl(IStepable newAi)
    {
        var chunkPos = Utilities.GetChunk(Utilities.GetBlockPos(newAi.Transform.position), ChunkWidth);
        if(LoadedChunks.TryGetValue(chunkPos, out var Chunk))
        {
            UnParentedAi.Remove(newAi);
            Chunk.AddChild(newAi);
        }
        else
        {
            UnParentedAi.Add(newAi);
        }
    }

    public static void Register(IStepable newAi)
    {
        ChunkManager.CurRealm.EntityContainer.AIManager.RegisterImpl(newAi);
    }

    public struct UpdateBlocksJob : IJob
    {
        [WriteOnly]
        public NativeHashMap<int2, BlockSliceData> blockDataMirror;

        [ReadOnly]
        public NativeQueue<(Vector2Int pos, BlockSliceData block)> blockChanges;

        public void Execute()
        {
            while (blockChanges.TryDequeue(out var info))
            {
                blockDataMirror[new int2(info.pos.x, info.pos.y)] = info.block;
            }
        }
    }
}
