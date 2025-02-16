using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


[Serializable]
public class Realm
{
    public delegate void BlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, BlockSlice block);
    public event BlockChanged OnBlockChanged;

    public delegate void BlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos);
    public event BlockRefreshed OnBlockRefreshed;

    public delegate void LightingUpdated(Dictionary<Vector3Int, int> updated);
    public event LightingUpdated OnLightingUpdated;

    public delegate void ChunkChanged(Chunk chunk);
    public event ChunkChanged OnChunkChanged;

    public string name;
    public ChunkGenerator Generator;

    ConcurrentDictionary<Vector2Int, Chunk> LoadedChunks = new ConcurrentDictionary<Vector2Int, Chunk>();
    
    
    private CancellationTokenSource CurGenToken;

    public EntityManager EntityContainer;
    Transform EntityContainerTransform;

    public void Initialize(GameObject entityContainerPrefab, Transform parent, int ChunkWidth)
    {
        EntityContainer = GameObject.Instantiate(entityContainerPrefab, parent).GetComponent<EntityManager>();
        EntityContainer.name = $"{name} Entity Container";
        EntityContainer.AIManager.Initialize(LoadedChunks, ChunkWidth);
        EntityContainerTransform = EntityContainer.transform;
    }

    public void Cleanup()
    {
        EntityContainer.AIManager.CleanUp();
    }

    public void SetContainerActive(bool active)
    {
        EntityContainer.gameObject.SetActive(active);
    }

    public void PlayerChangedChunks(Vector2Int curChunk, int chunkGenDistance, int ChunkGenWidth, CancellationToken AllTaskShutdown)
    {
        if (CurGenToken is not null)
        {
            CurGenToken.Cancel();
        }
        CurGenToken = new CancellationTokenSource();
        EntityContainer.AIManager.OnChunkChanged(curChunk);
        var newTask = Task.Run(() => GenerateNewChunks(curChunk, chunkGenDistance, ChunkGenWidth, CancellationTokenSource.CreateLinkedTokenSource(AllTaskShutdown, CurGenToken.Token).Token));
    }

    public async Task ChunkTick(Vector2Int curChunk, int chunkTickDistance, int msPerTick, CancellationToken AllTaskShutdown)
    {
        try
        {
            List<Task> chunkTasks = new List<Task>()
            {
                Task.Delay(msPerTick)
            };
            for (int x = -chunkTickDistance; x <= chunkTickDistance; x++)
            {
                for (int y = -chunkTickDistance; y <= chunkTickDistance; y++)
                {
                    if (LoadedChunks.TryGetValue(new Vector2Int(x, y) + curChunk, out var chunk))
                    {
                        chunkTasks.Add(Task.Run(() => chunk.ChunkTick(AllTaskShutdown)));
                    }
                }
            }
            await Task.WhenAll(chunkTasks);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public async Task GenerateNewChunks(Vector2Int curChunk, int dist, int ChunkWidth, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var newChunk in Utilities.Spiral(curChunk, (uint)dist))
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (!LoadedChunks.ContainsKey(newChunk))
                {
                    var chunk = new Chunk(newChunk, ChunkWidth);
                    await chunk.Generate(Generator);
                    LoadedChunks[newChunk] = chunk;
                    EntityContainer.AIManager.OnChunkChanged(chunk);
                    ConnectChunk(chunk);
                    chunk.SetParent(EntityContainerTransform);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void RefreshAllChunks()
    {
        foreach(var kvp in LoadedChunks)
        {
            Chunk_OnChunkChanged(kvp.Value);
        }
    }

    private void ConnectChunk(Chunk chunk)
    {
        chunk.OnBlockChanged += Chunk_OnBlockChanged;
        chunk.OnBlockRefreshed += Chunk_OnBlockRefreshed;
        chunk.OnChunkChanged += Chunk_OnChunkChanged;
        chunk.OnLightingUpdated += Chunk_OnLightingUpdated;
    }

    private void Chunk_OnLightingUpdated(Dictionary<Vector3Int, int> updated) => OnLightingUpdated?.Invoke(updated);

    private void Chunk_OnChunkChanged(Chunk chunk)
    {
        EntityContainer.AIManager.OnChunkChanged(chunk);
        OnChunkChanged?.Invoke(chunk);
    }

    private void Chunk_OnBlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, BlockSlice block)
    {
        EntityContainer.AIManager.OnBlockChanged(BlockPos, block);
        OnBlockChanged?.Invoke(chunk, BlockPos, ChunkPos, block);
    }

    private void Chunk_OnBlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos) => OnBlockRefreshed?.Invoke(chunk, BlockPos, ChunkPos);

    public T PerformChunkAction<T>(Vector2Int position, int ChunkWidth, Func<Chunk, Vector2Int, T> action , bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if(chunk.GetBlock(position).State is ProxyState proxy && useProxy)
            {
                return PerformChunkAction(proxy.ActualPos, ChunkWidth, action);
            }
            return action(chunk, position);
        }
        return default;
    }

    public bool TryGetChunk(Vector2Int chunk, out Chunk chunkObj)
    {
        return LoadedChunks.TryGetValue(chunk, out chunkObj);
    }

    public bool TryGetBlock(Vector2Int position, int ChunkWidth, out BlockSlice block, bool useProxy = true)
    {
        block = default;
        var chunkPos = Utilities.GetChunk(position, ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            block = chunk.GetBlock(position);
            if(useProxy && block.State is ProxyState proxy)
            {
                return TryGetBlock(proxy.ActualPos, ChunkWidth, out block);
            }
            return true;
        }
        return false;
    }
}
