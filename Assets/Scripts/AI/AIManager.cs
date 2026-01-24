using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;
using System.Collections.Concurrent;
using BlockDataRepos;
using NativeRealm;

public interface IAI
{
    public event Action<IAI> OnDespawn;
    public IPathFinder pathfinder { get; }
    public IBehavior behavior { get; }
    public Transform Transform { get; }
    public bool Natural { get; }
    public bool Hostile { get; }

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
    Vector2Int curChunk { get; set; }
    HashSet<IAI> UnParentedAi = new();
    Dictionary<Vector2Int, Chunk> SimulatedChunks = new();

    PathfindingManager PathFinder;
    AIBehaviorManager BehaviorManager;

    HashSet<IPathFinder> requestedPathfinders = new();

    public void Initialize(Dictionary<Vector2Int, Chunk> LoadedChunks, int ChunkWidth, RealmData worldData)
    {
        this.LoadedChunks = LoadedChunks;
        this.ChunkWidth = ChunkWidth;

        PathFinder = new PathfindingManager(worldData);
        BehaviorManager = new AIBehaviorManager();
    }

    public void CleanUp()
    {
        PathFinder.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        foreach(var ai in UnParentedAi.ToList())
        {
            RegisterImpl(ai);
        }
        foreach(var kvp in SimulatedChunks.ToList())
        {
            kvp.Value.EnableContainer(true);
        }
    }

    public void OnChunkChanged(Vector2Int curChunk)
    {
        this.curChunk = curChunk;
        var importantChunks = new HashSet<Vector2Int>(Utilities.Spiral(curChunk, (uint)AiSimDistance));
        foreach (var chunkPos in importantChunks)
        {
            if (!SimulatedChunks.TryGetValue(chunkPos, out var chunk) && ChunkManager.TryGetChunk(chunkPos, out chunk))
            {
                SimulatedChunks[chunkPos] = chunk;
                chunk.EnableContainer(true);
                chunk.SpawnAI();
            }
        }
        foreach (var chunk in SimulatedChunks.Where(chunk => !importantChunks.Contains(chunk.Key)).ToList())
        {
            chunk.Value.EnableContainer(false);
            SimulatedChunks.Remove(chunk.Key);
        }
    }

    public IEnumerator RunBehaviors()
    {
        while (true)
        {
            int count = 0;
            foreach (var kvp in SimulatedChunks.ToList())
            {
                var ais = kvp.Value.ais;
                BehaviorManager.RunBehaviors(ais.Select(ai => ai.behavior));
                count += ais.Count;
                if(count > AiPerEnumeration)
                {
                    yield return null;
                    count = 0;
                }
            }
            yield return null;
        }
    }

    public IEnumerator RunPathfinding()
    {
        while (true)
        {
            if (PathFinder != null)
            {
                var tmp = requestedPathfinders;
                requestedPathfinders = new();
                yield return PathFinder.RunPathfinders(tmp);
            }
            yield return null;
        }
    }

    public IEnumerator RunChunks()
    {
        while (true)
        {
            foreach (var kvp in SimulatedChunks.ToList())
            {
                var chunk = kvp.Value;
                if (chunk.ais.Count == 0) continue;
                foreach (var ai in chunk.ais.ToList())
                {
                    var newChunkPos = Utilities.GetChunk(Utilities.GetBlockPos(ai.Transform.position), ChunkWidth);
                    if (newChunkPos != chunk.ChunkPos && LoadedChunks.TryGetValue(newChunkPos, out var newChunk))
                    {
                        newChunk.AddChild(ai);
                        chunk.RemoveChild(ai);
                    }
                }
                yield return null;
            }
            yield return new WaitForSeconds(1);
        }
    }
    
    IEnumerator RunSpawnPass()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            if (GameSettings.NaturalSpawn)
            {
                foreach (var kvp in SimulatedChunks.ToList())
                {
                    if (kvp.Value.SpawnAI())
                    {
                        yield return null;
                    }
                }
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
            if (newAi.pathfinder != null) {
                newAi.pathfinder.RequestPathfinding += (ai) => requestedPathfinders.Add(ai);
            }
        }
        else
        {
            UnParentedAi.Add(newAi);
        }
        newAi.OnDespawn += (ai) => requestedPathfinders.Remove(ai.pathfinder);
    }

    private void OnEnable()
    {
        StartCoroutine(RunSpawnPass());
        StartCoroutine(RunChunks());
        StartCoroutine(RunPathfinding());
        StartCoroutine(RunBehaviors());
    }

    public static void Register(IAI newAi)
    {
        ChunkManager.CurRealm.EntityContainer.AIManager.RegisterImpl(newAi);
    }
}
