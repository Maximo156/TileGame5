using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using NativeRealm;
using BlockDataRepos;


[Serializable]
public class Realm
{
    public delegate void BlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, NativeBlockSlice block, BlockSliceState state);
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

    RealmData realmData;
    
    private CancellationTokenSource CurGenToken;

    public EntityManager EntityContainer;
    Transform EntityContainerTransform;

    Queue<(Chunk chunk, Vector2Int pos, Action<Chunk, Vector2Int> action)> QueuedActions = new();

    public void Step()
    {
        while(QueuedActions.TryDequeue(out var actionInfo))
        {
            actionInfo.action?.Invoke(actionInfo.chunk, actionInfo.pos);
        }
    }

    public void LateStep()
    {

    }

    public void Initialize(GameObject entityContainerPrefab, Transform parent, int ChunkWidth, int chunkGenRadius)
    {
        EntityContainer = GameObject.Instantiate(entityContainerPrefab, parent).GetComponent<EntityManager>();
        var distWithBuffer = chunkGenRadius * 2 + 3;
        realmData = new RealmData(ChunkWidth, distWithBuffer * distWithBuffer);
        EntityContainer.name = $"{name} Entity Container";
        EntityContainer.AIManager.Initialize(LoadedChunks, ChunkWidth, realmData);
        EntityContainerTransform = EntityContainer.transform;
    }

    public void Cleanup()
    {
        EntityContainer.AIManager.CleanUp();
        realmData.Dispose();
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

    public async Task GenerateNewChunks(Vector2Int curChunk, int dist, int ChunkWidth, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var newChunk in Utilities.Spiral(curChunk, (uint)dist))
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (!LoadedChunks.ContainsKey(newChunk))
                {
                    var chunkData = realmData.AddChunk(math.int2(newChunk.x, newChunk.y));
                    var chunk = new Chunk(newChunk, ChunkWidth, chunkData);
                    await chunk.Generate(Generator);
                    LoadedChunks[newChunk] = chunk;
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

    void DropChunk(Vector2Int chunk)
    {
        LoadedChunks.Remove(chunk, out var _);
        realmData.ClearChunk(math.int2(chunk.x, chunk.y));
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
        OnChunkChanged?.Invoke(chunk);
    }

    private void Chunk_OnBlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, NativeBlockSlice block, BlockSliceState state)
    {
        OnBlockChanged?.Invoke(chunk, BlockPos, ChunkPos, block, state);
    }

    private void Chunk_OnBlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos) => OnBlockRefreshed?.Invoke(chunk, BlockPos, ChunkPos);

    public T PerformChunkAction<T>(Vector2Int position, int ChunkWidth, Func<Chunk, Vector2Int, T> action , bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if(useProxy)
            {
                var offset = chunk.GetBlock(position).GetProxyOffset();
                if (offset != Vector2Int.zero)
                {
                    return PerformChunkAction(offset + position, ChunkWidth, action);
                }
            }
            return action(chunk, position);
        }
        return default;
    }

    public T QueueChunkAction<T>(Vector2Int position, int ChunkWidth, Action<Chunk, Vector2Int> action, Func<Chunk, Vector2Int, T> predictResult, bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if (useProxy)
            {
                var offset = chunk.GetBlock(position).GetProxyOffset();
                if (offset != Vector2Int.zero)
                {
                    return QueueChunkAction(offset + position, ChunkWidth, action, predictResult);
                }
            }
            QueuedActions.Enqueue((chunk, position, action));
            return predictResult(chunk, position);
        }
        return default;
    }

    public void QueueChunkAction(Vector2Int position, int ChunkWidth, Action<Chunk, Vector2Int> action, bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if (useProxy)
            {
                var offset = chunk.GetBlock(position).GetProxyOffset();
                if (offset != Vector2Int.zero)
                {
                    QueueChunkAction(offset + position, ChunkWidth, action);
                    return;
                }
            }
            Debug.LogWarning("Queue action");
            action(chunk, position);
        }
    }

    public bool TryGetChunk(Vector2Int chunk, out Chunk chunkObj)
    {
        return LoadedChunks.TryGetValue(chunk, out chunkObj);
    }

    public bool TryGetBlock(Vector2Int position, int ChunkWidth, out NativeBlockSlice block, out BlockSliceState state, bool useProxy = true)
    {
        block = default;
        state = default;
        var chunkPos = Utilities.GetChunk(position, ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            block = chunk.GetBlock(position);
            state = chunk.GetBlockState(position);
            var offset = block.GetProxyOffset();
            if (useProxy && offset != Vector2Int.zero)
            {
                return TryGetBlock(block.simpleBlockState.ToOffsetState() + position, ChunkWidth, out block, out state);
            }
            return true;
        }
        return false;
    }
}
