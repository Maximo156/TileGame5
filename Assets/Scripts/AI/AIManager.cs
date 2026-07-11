using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public interface IAI
{
    public event Action<IAI> OnDespawn;

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

    HashSet<IAI> UnParentedAi = new();
    Dictionary<Vector2Int, Chunk> SimulatedChunks = new();

    public void Initialize(Dictionary<Vector2Int, Chunk> LoadedChunks, int ChunkWidth)
    {
        this.LoadedChunks = LoadedChunks;
        this.ChunkWidth = ChunkWidth;
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
        var importantChunks = new HashSet<Vector2Int>(Utilities.Spiral(curChunk, (uint)AiSimDistance));
        foreach (var chunkPos in importantChunks)
        {
            if (!SimulatedChunks.TryGetValue(chunkPos, out var chunk) && ChunkManager.TryGetChunk(chunkPos, out chunk))
            {
                SimulatedChunks[chunkPos] = chunk;
                chunk.EnableContainer(true);
                if (GameSettings.NaturalSpawn)
                {
                    chunk.SpawnAI();
                }
            }
        }
        foreach (var chunk in SimulatedChunks.Where(chunk => !importantChunks.Contains(chunk.Key)).ToList())
        {
            chunk.Value.EnableContainer(false);
            SimulatedChunks.Remove(chunk.Key);
        }
    }

    public IEnumerator UpdateChunkChildren()
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
        }
        else
        {
            UnParentedAi.Add(newAi);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RunSpawnPass());
        StartCoroutine(UpdateChunkChildren());
    }

    public static void Register(IAI newAi)
    {
        ChunkManager.CurRealm.EntityContainer.AIManager.RegisterImpl(newAi);
    }
}
