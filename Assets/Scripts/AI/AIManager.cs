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
    public bool Natural { get; }

    public void Register()
    {
        AIManager.Register(this);
    }
}

public class AIManager : MonoBehaviour
{
    public int AiSimDistance = 10;
    public int AiPerEnumeration = 100;
    public int SpawnPassTime = 10;
    Dictionary<Vector2Int, Chunk> LoadedChunks;

    int ChunkWidth;
    public Vector2Int curChunk { get; set; }
    Queue<Vector2Int> SimQueue;
    HashSet<IAI> UnParentedAi = new();
    Dictionary<Vector2Int, Chunk> SimulatedChunks = new();

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
            print($"Simming from " + curChunk);
            SimQueue = new Queue<Vector2Int>(important);
            foreach(var chunk in SimulatedChunks.Where(chunk => !SimQueue.Contains(chunk.Key)).ToList())
            {
                chunk.Value.EnableContainer(false);
                SimulatedChunks.Remove(chunk.Key);
            }
        }

        if(!BahaviorsRunning && !PathfindingRunning)
        {
            var toRun = new List<IAI>();
            while(toRun.Count() < AiPerEnumeration && SimQueue.TryDequeue(out var chunkPos))
            {
                if(SimulatedChunks.TryGetValue(chunkPos, out var chunk) || ChunkManager.TryGetChunk(chunkPos, out chunk))
                {
                    chunk.EnableContainer(true);
                    SimulatedChunks[chunkPos] = chunk;
                    toRun.AddRange(chunk.ais);
                }
            }
            RunBehaviors(toRun);
            StartCoroutine(RunPathfinding(toRun));
        }
    }

    bool BahaviorsRunning;
    public void RunBehaviors(IEnumerable<IAI> ais)
    {
        BahaviorsRunning = true;
        BehaviorManager.RunBehaviors(ais.Select(ai => ai.behavior));
        BahaviorsRunning = false;
    }

    bool PathfindingRunning;
    public IEnumerator RunPathfinding(IEnumerable<IAI> ais)
    {
        PathfindingRunning = true;
        yield return PathFinder.RunPathfinders(ais.Select(ai => ai.pathfinder));
        PathfindingRunning = false;
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
    
    IEnumerator RunSpawnPass()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            var adjacent = Utilities.OctAdjacent.Select(v => curChunk + v);
            foreach(var kvp in SimulatedChunks.Where(kvp => !adjacent.Contains(kvp.Key)))
            {
                kvp.Value.SpawnAI();
            }
            yield return new WaitForSeconds(SpawnPassTime);
        }
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

    private void OnEnable()
    {
        StartCoroutine(RunSpawnPass());
    }

    public static void Register(IAI newAi)
    {
        ChunkManager.CurRealm.EntityContainer.AIManager.RegisterImpl(newAi);
    }
}
