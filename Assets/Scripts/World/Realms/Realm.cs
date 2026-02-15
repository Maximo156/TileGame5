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
    public delegate void BlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, NativeBlockSlice block, BlockItemStack state);
    public event BlockChanged OnBlockChanged;

    public delegate void BlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos);
    public event BlockRefreshed OnBlockRefreshed;

    public delegate void LightingUpdated(NativeArray<LightUpdateInfo> updated);
    public event LightingUpdated OnLightingUpdated;

    public delegate void ChunkChanged(Chunk chunk);
    public event ChunkChanged OnChunkChanged;

    public string name;
    public ChunkGenerator Generator;
    public bool Save;

    [Header("Realm Settings")]
    public RealmBiomeInfo BiomeInfo;
    public RealmStructureInfo StructureInfo;

    HashSet<Vector2Int> RequestedChunks = new();
    List<Vector2Int> DropChunks = new();
    List<IChunkLoadRequest> GenRequests = new();
    List<Vector2Int> NeedsInitialization = new();
    Dictionary<Vector2Int, Chunk> LoadedChunks = new Dictionary<Vector2Int, Chunk>();

    RealmData realmData;

    public EntityManager EntityContainer {  get; private set; }

    Queue<(Chunk chunk, Vector2Int pos, Action<Chunk, Vector2Int> action)> QueuedActions = new();

    readonly ProfilerMarker p_Step = new ProfilerMarker("Realm.Step");
    readonly ProfilerMarker p_LateStep = new ProfilerMarker("Realm.Step");

    NativeList<int2> frameUpdatedChunks;
    LightJobInfo lightJobInfo;
    ChunkTickJobInfo tickJobInfo;

    JobHandle ReadDependencies;

    public void Step(Vector2Int playerCurrentChunk)
    {
        frameUpdatedChunks = new NativeList<int2>(0, Allocator.TempJob);
        while (QueuedActions.TryDequeue(out var actionInfo))
        {
            actionInfo.action?.Invoke(actionInfo.chunk, actionInfo.pos);
            frameUpdatedChunks.Add(actionInfo.chunk.ChunkPos.ToInt());
        }
        ProcessDropChunks();
        p_Step.Auto();

        ProcessGenRequests(frameUpdatedChunks).Complete();

        // Scheduel native reads
        lightJobInfo = LightCalculation.ScheduelLightUpdate(realmData, frameUpdatedChunks);
        tickJobInfo = ChunkTick.ScheduelTick(tickJobInfo, playerCurrentChunk, realmData);
         
        ReadDependencies = frameUpdatedChunks.Dispose(JobHandle.CombineDependencies(lightJobInfo.jobHandle, tickJobInfo.job, EntityContainer.AIManager.RunPathfinding()));

        SaveChunks();
    }

    public void LateStep()
    {
        ChunkSaver.Flush();
        var prof = p_LateStep.Auto();
        var updates = new NativeQueue<LightUpdateInfo>(Allocator.TempJob);

        LightCalculation.CopyLight(realmData, lightJobInfo, updates, ReadDependencies).Complete();

        if (tickJobInfo.needsProcessing)
        {
            ChunkTick.WriteUpdates(tickJobInfo, realmData).Complete();

            while (tickJobInfo.updates.TryDequeue(out var update))
            {
                if (LoadedChunks.TryGetValue(update.chunk.ToVector(), out var c))
                {
                    c.TriggerChangedBlock(update.localPos.ToVector());
                }
            };
            tickJobInfo.Dispose();
        }

        var updateArray = updates.ToArray(Allocator.TempJob);
        OnLightingUpdated?.Invoke(updateArray);
        updates.Dispose();
        updateArray.Dispose();
        InitializeManagedChunks();
        EntityContainer.AIManager.ProcessPathfinding();
    }

    float lastSave;
    void SaveChunks()
    {
        if (Time.time - lastSave < 1) return;
        lastSave = Time.time;
        foreach (var c in ToSave)
        {
            if (LoadedChunks.TryGetValue(c, out var chunk))
            {
                ChunkSaver.SaveChunk(name, chunk);
            }
        }
        ToSave.Clear();
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
        if(GenRequests.Count == 0)
        {
            return default;
        }

        var complete = new List<IChunkLoadRequest>();
        var notCompleted = new List<IChunkLoadRequest>();
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
            dep = request.CopyAndDispose(realmData, RequestedChunks, NeedsInitialization, updatedChunks, LoadedChunkData, dep);
        }
        return dep;
    }

    Dictionary<Vector2Int, Chunk> LoadedChunkData = new();
    void InitializeManagedChunks()
    {
        foreach(var c in NeedsInitialization)
        {
            var newChunk = new Chunk(c, WorldConfig.ChunkWidth, realmData.GetChunk(c.ToInt()), this);
            if(LoadedChunkData.Remove(c, out var loadedChunk))
            {
                newChunk.BlockItems = loadedChunk.BlockItems;
                newChunk.BlockStates = loadedChunk.BlockStates;
            }
            ConnectChunk(newChunk);
            newChunk.InitCache();
            LoadedChunks.Add(c, newChunk);
        }
        LoadedChunkData.Clear();
        NeedsInitialization.Clear();
    }

    public void Initialize(GameObject entityContainerPrefab, Transform parent)
    {
        EntityContainer = GameObject.Instantiate(entityContainerPrefab, parent).GetComponent<EntityManager>();
        var distWithBuffer = WorldConfig.ChunkGenDistance * 2 + 3;
        realmData = new RealmData(WorldConfig.ChunkWidth, distWithBuffer * distWithBuffer);
        EntityContainer.name = $"{name} Entity Container";
        EntityContainer.AIManager.Initialize(LoadedChunks, WorldConfig.ChunkWidth, realmData);
    }

    public void Cleanup()
    {
        realmData.Dispose(); 

        foreach (var request in GenRequests)
        {
            request.Dispose();
        }

        BiomeInfo.Dispose();
        StructureInfo.Dispose();
    }

    public void Disable()
    {
        EntityContainer.gameObject.SetActive(false);
    }

    public void Enable()
    {
        EntityContainer.gameObject.SetActive(true);
    }

    public void PlayerChangedChunks(Vector2Int curChunk, CancellationToken AllTaskShutdown)
    {
        EntityContainer.AIManager.OnChunkChanged(curChunk);
        CalcValidChunks(curChunk);
    }

    public void CalcValidChunks(Vector2Int curChunk)
    {
        var curDesiredChunks = new HashSet<Vector2Int>(Utilities.Spiral(curChunk, (uint)WorldConfig.ChunkGenDistance));
        var newRequestedChunks = new NativeList<int2>(10, Allocator.Persistent);
        var toLoadFromFileChunks = new NativeList<int2>(10, Allocator.Persistent);
        foreach (var chunk in curDesiredChunks)
        {
            if (!LoadedChunks.ContainsKey(chunk) && !RequestedChunks.Contains(chunk))
            {
                RequestedChunks.Add(chunk);
                if (Save && ChunkSaver.HasSavedVersion(name, chunk))
                {
                    toLoadFromFileChunks.Add(chunk.ToInt());
                }
                else
                {
                    newRequestedChunks.Add(chunk.ToInt());
                }
            }
        }

        if (newRequestedChunks.Length > 0)
        {
            GenRequests.Add(new ChunkGenRequest(newRequestedChunks, Generator, new() { BiomeInfo = BiomeInfo, StructureInfo = StructureInfo }));
        }
        else
        {
            newRequestedChunks.Dispose();
        }

        if (toLoadFromFileChunks.Length > 0)
        {
            GenRequests.Add(ChunkSaver.LoadChunks(name, toLoadFromFileChunks));
        }
        else
        {
            toLoadFromFileChunks.Dispose();
        }

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
        LoadedChunks.Remove(chunk, out var c);
        c.Drop();
    }

    public async Task ChunkManagedTick(Vector2Int curChunk, CancellationToken AllTaskShutdown)
    {
        try
        {
            var timer = Task.Delay(WorldConfig.TickMs);
            var tickDistance = WorldConfig.ChunkTickDistance;
            for (int x = -tickDistance; x <= tickDistance; x++)
            {
                for (int y = -tickDistance; y <= tickDistance; y++)
                {
                    if (LoadedChunks.TryGetValue(new Vector2Int(x, y) + curChunk, out var chunk))
                    {
                        chunk.ChunkTick();
                    }
                }
            }
            await timer;
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
        chunk.OnSaveableEvent += SaveChunk;
    }

    private void Chunk_OnChunkChanged(Chunk chunk)
    {
        OnChunkChanged?.Invoke(chunk);
    }

    private HashSet<Vector2Int> ToSave = new();

    private void Chunk_OnBlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, NativeBlockSlice block, BlockItemStack state)
    {
        OnBlockChanged?.Invoke(chunk, BlockPos, ChunkPos, block, state);
    }

    private void Chunk_OnBlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos)
    {
        OnBlockRefreshed?.Invoke(chunk, BlockPos, ChunkPos);
    }

    void SaveChunk(Vector2Int ChunkPos)
    {
        if (Save)
        {
            ToSave.Add(ChunkPos);
        }
    }

    public T PerformChunkAction<T>(Vector2Int position, Func<Chunk, Vector2Int, T> action , bool useProxy = true)
    {
        var chunkPos = Utilities.GetChunk(position, WorldConfig.ChunkWidth);
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
        var chunkPos = Utilities.GetChunk(position, WorldConfig.ChunkWidth);
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
        var chunkPos = Utilities.GetChunk(position, WorldConfig.ChunkWidth);
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
            QueuedActions.Enqueue((chunk, position, action));
        }
    }

    public bool TryGetChunk(Vector2Int chunk, out Chunk chunkObj)
    {
        return LoadedChunks.TryGetValue(chunk, out chunkObj);
    }

    public bool TryGetBlockAndState(Vector2Int position, out NativeBlockSlice block, out BlockState state, bool useProxy = true)
    {
        block = default;
        state = default;
        var chunkPos = Utilities.GetChunk(position, WorldConfig.ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            block = chunk.GetBlock(position);
            state = chunk.GetBlockState(position);
            var offset = block.GetProxyOffset();
            if (useProxy && offset != Vector2Int.zero)
            {
                return TryGetBlockAndState(block.simpleBlockState.ToOffsetState() + position, out block, out state);
            }
            return true;
        }
        return false;
    }

    public bool TryGetBlock(Vector2Int position, out NativeBlockSlice block, bool useProxy = true)
    {
        block = default;
        var chunkPos = Utilities.GetChunk(position, WorldConfig.ChunkWidth);
        if (LoadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            block = chunk.GetBlock(position);
            var offset = block.GetProxyOffset();
            if (useProxy && offset != Vector2Int.zero)
            {
                return TryGetBlock(block.simpleBlockState.ToOffsetState() + position, out block);
            }
            return true;
        }
        return false;
    }

    struct ChunkGenRequest : IChunkLoadRequest
    {
        public NativeList<int2> chunks { get; private set; }

        JobHandle handle;
        public RealmData realmData { get; private set; }

        public bool isComplete => handle.IsCompleted;

        public ChunkGenRequest(NativeList<int2> chunks, ChunkGenerator generator, RealmInfo realmInfo)
        {
            this.chunks = chunks;
            realmData = default;
            (realmData, handle) = generator.GetGenJob(WorldConfig.ChunkWidth, this.chunks, realmInfo);
        }

        public void Complete()
        {
            handle.Complete();
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
        var chunkWidth = WorldConfig.ChunkWidth;
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

public interface IChunkLoadRequest
{
    public bool isComplete { get; }

    public RealmData realmData { get; }
    public NativeList<int2> chunks { get; }

    public JobHandle CopyAndDispose(RealmData targetData, HashSet<Vector2Int> requestedChunks, List<Vector2Int> needInitialization, NativeList<int2> updatedChunks, Dictionary<Vector2Int, Chunk> managedChunkInfo, JobHandle dep)
    {
        if (!isComplete) throw new InvalidOperationException("Only copy once handle is completed");
        Complete();
        var copyJob = targetData.CopyFrom(realmData, chunks, dep);
        foreach (var c in chunks)
        {
            updatedChunks.Add(c);
            var v = c.ToVector();
            requestedChunks.Remove(v);
            needInitialization.Add(v);
        }
        CopyManagedChunks(managedChunkInfo);
        return JobHandle.CombineDependencies(realmData.Dispose(copyJob), chunks.Dispose(copyJob));
    }

    public void Complete();

    public void Dispose();

    public void CopyManagedChunks(Dictionary<Vector2Int, Chunk> output)
    {

    }
}
