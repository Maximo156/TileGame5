using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public interface IStepable
{
    public Vector2Int Step(float deltaTime);

    public Transform Transform { get; }
}

public class AIManager : MonoBehaviour
{
    public int AiSimDistance = 10;
    Dictionary<Vector2Int, Chunk> LoadedChunks;
    int ChunkWidth;
    public Vector2Int curChunk { get; set; }
    Queue<Vector2Int> SimQueue;

    HashSet<IStepable> UnParentedAi = new();
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

    bool running;
    public IEnumerator RunChunk(Chunk chunk)
    {
        running = true;
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
}
