using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

public interface IAI
{
    public IPathFinder pathfinder { get; }
    public IBehavior behavior { get; }
    public Transform Transform { get; }
}

public class AIManager : MonoBehaviour
{
    public int AiSimDistance = 10;
    Dictionary<Vector2Int, Chunk> LoadedChunks;

    int ChunkWidth;
    public Vector2Int curChunk { get; set; }
    Queue<Vector2Int> SimQueue;
    HashSet<IAI> UnParentedAi = new();

    PathfindingManager PathFinder;
    AIBehaviorManager BehaviorManager;

    public void Initialize(Dictionary<Vector2Int, Chunk> LoadedChunks, int ChunkWidth)
    {
        this.LoadedChunks = LoadedChunks;
        this.ChunkWidth = ChunkWidth;

        PathFinder = new PathfindingManager();
        BehaviorManager = new AIBehaviorManager();
    }

    public void CleanUp()
    {
        PathFinder.Dispose();
    }

    public void OnBlockChanged(Vector2Int worldPos, BlockSlice block) => PathFinder.OnBlockChanged(worldPos, block);

    public void OnChunkChanged(Chunk chunk) => PathFinder.OnChunkChanged(chunk);

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
                StartCoroutine(RunChunk(chunk));
            }
        }
    }

    bool running;
    public IEnumerator RunChunk(Chunk chunk)
    {
        running = true;

        yield return PathFinder.RunChunk(chunk.ais.Select(ai => ai.pathfinder));

        yield return BehaviorManager.RunChunk(chunk.ais.Select(ai => ai.behavior));

        foreach (var ai in chunk.ais.ToList())
        {
            var newChunkPos = Utilities.GetChunk(Utilities.GetBlockPos(ai.Transform.position), ChunkWidth);
            if (newChunkPos != chunk.ChunkPos && LoadedChunks.TryGetValue(newChunkPos, out var newChunk))
            {
                chunk.ais.Remove(ai);
                newChunk.AddChild(ai);
            }
        }

        running = false;
    }

    private void RegisterImpl(IAI newAi)
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

    public static void Register(IAI newAi)
    {
        ChunkManager.CurRealm.EntityContainer.AIManager.RegisterImpl(newAi);
    }
}
