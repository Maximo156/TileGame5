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
using Unity.Jobs;
using System.Linq;
using Unity.Profiling;


[Serializable]
public class Realm
{
    public delegate void BlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, NativeBlockSlice block, BlockSliceState state);
    public event BlockChanged OnBlockChanged;

    public delegate void BlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos);
    public event BlockRefreshed OnBlockRefreshed;

    public delegate void LightingUpdated(NativeArray<LightUpdateInfo> updated);
    public event LightingUpdated OnLightingUpdated;

    public delegate void ChunkChanged(Chunk chunk);
    public event ChunkChanged OnChunkChanged;

    public string name;
    public ChunkGenerator Generator;

    [Header("Realm Settings")]
    public RealmBiomeInfo BiomeInfo;
    public RealmStructureInfo StructureInfo;

    HashSet<Vector2Int> RequestedChunks = new();
    List<Vector2Int> DropChunks = new();
    List<ChunkGenRequest> GenRequests = new();
    List<Vector2Int> NeedsInitialization = new();
    Dictionary<Vector2Int, Chunk> LoadedChunks = new Dictionary<Vector2Int, Chunk>();

    RealmData realmData;

    public EntityManager EntityContainer {  get; private set; }
    Transform EntityContainerTransform;

    Queue<(Chunk chunk, Vector2Int pos, Action<Chunk, Vector2Int> action)> QueuedActions = new();

    readonly ProfilerMarker p_Step = new ProfilerMarker("Realm.Step");
    readonly ProfilerMarker p_LateStep = new ProfilerMarker("Realm.Step");

    NativeList<int2> frameUpdatedChunks;
    LightJobInfo lightJobInfo;
    public void Step()
    {
        var prof = p_Step.Auto();
        frameUpdatedChunks = new NativeList<int2>(0, Allocator.TempJob);
        while (QueuedActions.TryDequeue(out var actionInfo))
        {
            actionInfo.action?.Invoke(actionInfo.chunk, actionInfo.pos);
            frameUpdatedChunks.Add(actionInfo.chunk.ChunkPos.ToInt());
        }

        ProcessDropChunks();

        ProcessGenRequests(frameUpdatedChunks).Complete();

        // Scheduel native reads
        lightJobInfo = LightCalculation.ScheduelLightUpdate(realmData, frameUpdatedChunks);

        // Initialize new chunks
        InitializeManagedChunks();
    }

    void ProcessDropChunks()
    {
        foreach(var c in DropChunks)
        {
            realmData.ClearChunk(c.ToInt());
        }
        DropChunks.Clear();
    }
     
    JobHandle ProcessGenRequests(NativeList<int2> updatedChunks)
    {
        var complete = new List<ChunkGenRequest>();
        var notCompleted = new List<ChunkGenRequest>();
        foreach(var request in GenRequests)
        {
            if (request.isComplete)
            {
                complete.Add(request);
            }
            else
            {
                notCompleted.Add(request);
            }
        }
        GenRequests = notCompleted;

        var dep = new JobHandle();
        foreach(var request in complete)
        {
            dep = request.CopyAndDispose(realmData, RequestedChunks, NeedsInitialization, updatedChunks, dep);
        }
        return dep;
    }

    void InitializeManagedChunks()
    {
        foreach(var c in NeedsInitialization)
        {
            var newChunk = new Chunk(c, WorldSettings.ChunkWidth, realmData.AddChunk(c.ToInt()), this);
            ConnectChunk(newChunk);
            LoadedChunks.Add(c, newChunk);
        }
        NeedsInitialization.Clear();
    }

    public void LateStep()
    {
        var prof = p_LateStep.Auto();
        var updates = new NativeQueue<LightUpdateInfo>(Allocator.TempJob);

        var copyLightJob = LightCalculation.CopyLight(realmData, lightJobInfo, updates);

        JobHandle.CombineDependencies(frameUpdatedChunks.Dispose(copyLightJob), lightJobInfo.Dispose(copyLightJob)).Complete();
        var updateArray = updates.ToArray(Allocator.TempJob);
        OnLightingUpdated?.Invoke(updateArray);
        updates.Dispose();
        updateArray.Dispose();
    }

    public void Initialize(GameObject entityContainerPrefab, Transform parent)
    {
        EntityContainer = GameObject.Instantiate(entityContainerPrefab, parent).GetComponent<EntityManager>();
        var distWithBuffer = WorldSettings.ChunkGenDistance * 2 + 3;
        realmData = new RealmData(WorldSettings.ChunkWidth, distWithBuffer * distWithBuffer);
        EntityContainer.name = $"{name} Entity Container";
        EntityContainer.AIManager.Initialize(LoadedChunks, WorldSettings.ChunkWidth, realmData);
        EntityContainerTransform = EntityContainer.transform;
    }

    public void Cleanup()
    {
        EntityContainer.AIManager.CleanUp(); 
        realmData.Dispose(); 

        foreach (var request in GenRequests)
        {
            request.Dispose();
        }

        lightJobInfo.Dispose();
        BiomeInfo.Dispose();
        StructureInfo.Dispose();
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
        var newRequestedChunks = new NativeList<int2>(10, Allocator.Persistent);
        foreach(var chunk in curDesiredChunks)
        {
            if (!LoadedChunks.ContainsKey(chunk) && !RequestedChunks.Contains(chunk))
            {
                newRequestedChunks.Add(chunk.ToInt());
            }
        }

        GenRequests.Add(new ChunkGenRequest(newRequestedChunks, Generator, RequestedChunks, new() { BiomeInfo = BiomeInfo, StructureInfo = StructureInfo }));
        
        foreach (var key in LoadedChunks.Keys)
        {
            if (!curDesiredChunks.Contains(key))
            {
                DropChunks.Add(key);
            }
        }

        foreach (var key in DropChunks)
        {
            DropManagedChunk(key);
        }
    }

    void DropManagedChunk(Vector2Int chunk)
    {
        LoadedChunks.Remove(chunk, out var _);
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
    }

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

    struct ChunkGenRequest
    {
        NativeList<int2> chunks;
        JobHandle handle;
        RealmData realmData;

        public bool isComplete => handle.IsCompleted;

        public ChunkGenRequest(NativeList<int2> chunks, ChunkGenerator generator, HashSet<Vector2Int> requestedChunks, RealmInfo realmInfo)
        {
            this.chunks = chunks;
            realmData = default;
            (realmData, handle) = generator.GetGenJob(WorldSettings.ChunkWidth, this.chunks, realmInfo);
            foreach(var c in chunks)
            {
                requestedChunks.Add(c.ToVector());
            }
        }
        } 

        public JobHandle CopyAndDispose(RealmData targetData, HashSet<Vector2Int> requestedChunks, List<Vector2Int> needInitialization, NativeList<int2> updatedChunks, JobHandle dep)
        {
            if (!handle.IsCompleted) throw new InvalidOperationException("Only copy once handle is completed");
            handle.Complete();
            var copyJob = targetData.CopyFrom(realmData, chunks, dep);
            foreach (var c in chunks)
            {
                updatedChunks.Add(c);
                var v = c.ToVector();
                requestedChunks.Remove(v);
                needInitialization.Add(v);
            }

            return JobHandle.CombineDependencies(realmData.Dispose(copyJob), chunks.Dispose(copyJob)); 
        }

        public void Dispose()
        {
            handle.Complete();
            realmData.Dispose();
            chunks.Dispose();
        }
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
            //Gizmos.DrawWireCube((key * chunkWidth + chunkWidth / 2 * Vector2Int.one).ToVector3Int(), chunkWidth * Vector3.one);
        }
        Gizmos.color = Color.blue;
        foreach (var key in RequestedChunks)
        {
            Gizmos.DrawWireCube((key * chunkWidth + chunkWidth / 2 * Vector2Int.one).ToVector3Int(), chunkWidth * Vector3.one);
        }
    }
}
