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

    public void Register()
    {
        AIManager.Register(this);
    }
}

public class AIManager : MonoBehaviour
{
    public int AiSimDistance = 10;
    public int AiPerEnumeration = 100;
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

        var important = Utilities.Spiral(curChunk, (uint)AiSimDistance);

        if (!chunksRunning)
        {
            StartCoroutine(RunChunks(important));
        }

        if (SimQueue == null || SimQueue.Count == 0)
        {
            SimQueue = new Queue<Vector2Int>(important);
        }

        if(!AiRunning)
        {
            var toRun = new List<IAI>();
            while(toRun.Count() < AiPerEnumeration && SimQueue.TryDequeue(out var chunkPos))
            {
                if(ChunkManager.TryGetChunk(chunkPos, out var chunk))
                {
                    toRun.AddRange(chunk.ais);
                }
            }
            StartCoroutine(RunAis(toRun));
        }
    }

    bool AiRunning;
    public IEnumerator RunAis(IEnumerable<IAI> ais)
    {
        AiRunning = true;
        yield return PathFinder.RunChunk(ais.Select(ai => ai.pathfinder));

        yield return BehaviorManager.RunChunk(ais.Select(ai => ai.behavior));
        AiRunning = false;
    }

    bool chunksRunning;
    public IEnumerator RunChunks(IEnumerable<Vector2Int> chunks)
    {
        chunksRunning = true;
        foreach (var pos in chunks)
        {
            if (LoadedChunks.TryGetValue(pos, out var chunk))
            {
                if (chunk.ais.Count == 0) continue;
                foreach (var ai in chunk.ais.ToList())
                {
                    var newChunkPos = Utilities.GetChunk(Utilities.GetBlockPos(ai.Transform.position), ChunkWidth);
                    if (newChunkPos != chunk.ChunkPos && LoadedChunks.TryGetValue(newChunkPos, out var newChunk))
                    {
                        chunk.ais.Remove(ai);
                        newChunk.AddChild(ai);
                    }
                }

                yield return null;
            }
        }
        chunksRunning = false;
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
