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

    HashSet<Vector2Int> DesiredLoadedChunks = new HashSet<Vector2Int>();
    ConcurrentQueue<Vector2Int> RequestedChunks = new ConcurrentQueue<Vector2Int>();
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

    public void Initialize(GameObject entityContainerPrefab, Transform parent)
    {
        EntityContainer = GameObject.Instantiate(entityContainerPrefab, parent).GetComponent<EntityManager>();
        var distWithBuffer = WorldSettings.ChunkGenDistance * 2 + 3;
        realmData = new RealmData(WorldSettings.ChunkWidth, distWithBuffer * distWithBuffer);
        EntityContainer.name = $"{name} Entity Container";
        EntityContainer.AIManager.Initialize(LoadedChunks, WorldSettings.ChunkWidth, realmData);
        EntityContainerTransform = EntityContainer.transform;

        CurGenToken = new CancellationTokenSource();
        Task.Run(() => RunChunkGen());
    }

    public void Cleanup()
    {
        EntityContainer.AIManager.CleanUp();
        realmData.Dispose();
        CurGenToken.Cancel();
    }

    public void SetContainerActive(bool active)
    {
        EntityContainer.gameObject.SetActive(active);
    }

    public void PlayerChangedChunks(Vector2Int curChunk, CancellationToken AllTaskShutdown)
    {
        EntityContainer.AIManager.OnChunkChanged(curChunk);
        CalcValidChunks(curChunk);
    }

    public void CalcValidChunks(Vector2Int curChunk)
    {
        var curDesiredChunks = new HashSet<Vector2Int>(Utilities.Spiral(curChunk, (uint)WorldSettings.ChunkGenDistance));
        var toRequest = new Queue<Vector2Int>();
        var toDrop = new HashSet<Vector2Int>();
        foreach(var chunk in curDesiredChunks)
        {
            if (!DesiredLoadedChunks.Contains(chunk))
            {
                Debug.Log($"requesting {chunk}");
                toRequest.Enqueue(chunk);
            }
        }
        DesiredLoadedChunks = curDesiredChunks;
        while(toRequest.TryDequeue(out var r))
        {
            Debug.Log($"Enqueue {r}");
            RequestedChunks.Enqueue(r);
        }
        foreach (var key in LoadedChunks.Keys)
        {
            if (!curDesiredChunks.Contains(key))
            {
                toDrop.Add(key);
            }
        }

        foreach (var key in toDrop)
        {
            DropChunk(key);
        }
    }

    public async Task RunChunkGen()
    {
        Debug.Log("Starting gen thread");
        while (!CurGenToken.IsCancellationRequested)
        {
            if (RequestedChunks.Count > 0)
            {
                try
                {
                    while (RequestedChunks.TryDequeue(out var newChunk))
                    {
                        if (DesiredLoadedChunks.Contains(newChunk) && !LoadedChunks.ContainsKey(newChunk))
                        {
                            var chunkData = realmData.AddChunk(math.int2(newChunk.x, newChunk.y));
                            var chunk = new Chunk(newChunk, WorldSettings.ChunkWidth, chunkData);
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
            else
            {
                await Task.Delay(100);
            }
        }
        Debug.Log("Stopping gen thread");
    }

    void DropChunk(Vector2Int chunk)
    {
        LoadedChunks.Remove(chunk, out var _);
        realmData.ClearChunk(math.int2(chunk.x, chunk.y));
    }

    public async Task ChunkTick(Vector2Int curChunk, CancellationToken AllTaskShutdown)
    {
        try
        {
            List<Task> chunkTasks = new List<Task>()
            {
                Task.Delay(WorldSettings.TickMs)
            };
            var tickDistance = WorldSettings.ChunkTickDistance;
            for (int x = -tickDistance; x <= tickDistance; x++)
            {
                for (int y = -tickDistance; y <= tickDistance; y++)
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

    public T PerformChunkAction<T>(Vector2Int position, Func<Chunk, Vector2Int, T> action , bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, WorldSettings.ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if(useProxy)
            {
                var offset = chunk.GetBlock(position).GetProxyOffset();
                if (offset != Vector2Int.zero)
                {
                    return PerformChunkAction(offset + position, action);
                }
            }
            return action(chunk, position);
        }
        return default;
    }

    public T QueueChunkAction<T>(Vector2Int position, Action<Chunk, Vector2Int> action, Func<Chunk, Vector2Int, T> predictResult, bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, WorldSettings.ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if (useProxy)
            {
                var offset = chunk.GetBlock(position).GetProxyOffset();
                if (offset != Vector2Int.zero)
                {
                    return QueueChunkAction(offset + position, action, predictResult);
                }
            }
            QueuedActions.Enqueue((chunk, position, action));
            return predictResult(chunk, position);
        }
        return default;
    }

    public void QueueChunkAction(Vector2Int position, Action<Chunk, Vector2Int> action, bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, WorldSettings.ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            if (useProxy)
            {
                var offset = chunk.GetBlock(position).GetProxyOffset();
                if (offset != Vector2Int.zero)
                {
                    QueueChunkAction(offset + position, action);
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

    public bool TryGetBlock(Vector2Int position, out NativeBlockSlice block, out BlockSliceState state, bool useProxy = true)
    {
        block = default;
        state = default;
        var chunkPos = Utilities.GetChunk(position, WorldSettings.ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            block = chunk.GetBlock(position);
            state = chunk.GetBlockState(position);
            var offset = block.GetProxyOffset();
            if (useProxy && offset != Vector2Int.zero)
            {
                return TryGetBlock(block.simpleBlockState.ToOffsetState() + position, out block, out state);
            }
            return true;
        }
        return false;
    }

    public void DrawDebug()
    {
        var chunkWidth = WorldSettings.ChunkWidth;
        Gizmos.color = Color.green;
        foreach (var key in LoadedChunks.Keys)
        {
            Gizmos.DrawWireCube((key * chunkWidth + chunkWidth / 2 * Vector2Int.one).ToVector3Int(), chunkWidth * Vector3.one);
        }
        Gizmos.color = Color.yellow;
        foreach (var key in RequestedChunks)
        {
            Gizmos.DrawWireCube((key * chunkWidth + chunkWidth / 2 * Vector2Int.one).ToVector3Int(), chunkWidth * Vector3.one);
        }
        Gizmos.color = Color.blue;
        foreach (var key in DesiredLoadedChunks)
        {
            Gizmos.DrawWireCube((key * chunkWidth + chunkWidth / 2 * Vector2Int.one).ToVector3Int(), chunkWidth * Vector3.one);
        }
    }
}
