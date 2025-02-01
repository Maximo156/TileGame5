using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public class ChunkManager : MonoBehaviour
{
    public delegate void RealmChange(Realm oldRealm, Realm newRealm);
    public static event RealmChange OnRealmChange;

    static ChunkManager Manager;
    public static int ChunkWidth => Manager.chunkWidth;
    public static int MsPerTick => Manager.msPerTick;
    public static Realm CurRealm => Manager.ActiveRealm;
    public static string DataPath;

    [Header("Realm Settings")]
    public string startingRealm;
    public List<Realm> Realms;
    public GameObject EntityContainerPrefab;

    [Header("World Settings")]
    public int chunkWidth;
    public int chunkGenDistance;
    public int chunkTickDistance;
    public int msPerTick = 30;
    public bool debug;

    Realm _activeRealm;
    Realm ActiveRealm
    {
        get => _activeRealm;
        set {
            _activeRealm?.SetContainerActive(false);
            OnRealmChange?.Invoke(_activeRealm, value);
            _activeRealm = value;
            _activeRealm.PlayerChangedChunks(Vector2Int.zero, chunkGenDistance, chunkWidth, AllTaskShutdown.Token);
            _activeRealm.RefreshAllChunks();
            _activeRealm?.SetContainerActive(true);
        }
    }

    public void Awake()
    { 
        Manager = this;
        PlayerMovement.OnPlayerChangedChunks += PlayerChangedChunks;
        PortalBlock.OnPortalBlockUsed += PortalUsed;
        DataPath = Application.persistentDataPath;
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach(var realm in Realms)
        {
            realm.Initialize(EntityContainerPrefab, transform, chunkWidth);
            realm.SetContainerActive(false);
        }
        ActiveRealm = Realms.First(r => r.name == startingRealm);
        Task.Run(() => ChunkTick());
    }

    private CancellationTokenSource AllTaskShutdown = new CancellationTokenSource();

    Vector2Int currentChunk;
    private void PlayerChangedChunks(Vector2Int curChunk)
    {
        currentChunk = curChunk;
        ActiveRealm.PlayerChangedChunks(currentChunk, chunkGenDistance, chunkWidth, AllTaskShutdown.Token);
    }

    public async void ChunkTick()
    {
        while (true && !AllTaskShutdown.Token.IsCancellationRequested)
        {
            await ActiveRealm.ChunkTick(currentChunk, chunkTickDistance, msPerTick, AllTaskShutdown.Token);
        }
        Debug.Log("Stopping Tick");
    }

    public static bool TryGetBlock(Vector2Int position, out BlockSlice block, bool useProxy = true)
    {
        block = default;
        if (Manager == null) return false;
        return Manager.ActiveRealm.TryGetBlock(position, ChunkWidth, out block, useProxy);
    }

    public static BlockSlice GetBlock(Vector2Int position)
    {
        if(!TryGetBlock(position, out var block))
        {
            throw new InvalidOperationException("Checking ungenerated chunk");
        }
        return block;
    }

    private void PortalUsed(string newDim, PortalBlock exitBlock, Vector2Int worldPos)
    {
        ActiveRealm = Realms.First(r => r.name == newDim);
        int count = 0;
        while(!PlaceBlock(worldPos, Vector2Int.zero, exitBlock, true) && count++ < 1000)
        {
            Thread.Sleep(1);
        }
    }

    public static float GetMovementSpeed(Vector2Int position)
    {
        if (!TryGetBlock(position, out var block)) return 0;
        return block.MovementSpeed;
    }

    public static bool TryGetChunk(Vector2Int chunk, out Chunk chunkObj)
    {
        return Manager.ActiveRealm.TryGetChunk(chunk, out chunkObj);
    }

    public static bool PlaceBlock(Vector2Int position, Vector2Int dir, Block block, bool force = false)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, ChunkWidth, (chunk, pos) => chunk.PlaceBlock(pos, dir, block, force));
    }

    public static bool Interact(Vector2Int position)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, ChunkWidth, (chunk, pos) => chunk.Interact(pos));
    }

    public static bool BreakBlock(Vector2Int position, bool roof, bool drop = true)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, ChunkWidth, (chunk, pos) => chunk.BreakBlock(pos, roof, drop));
    }

    public static bool PlaceItem(Vector2Int position, ItemStack item)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, ChunkWidth, (chunk, pos) => chunk.PlaceItem(pos, item));
    }

    public static ItemStack PopItem(Vector2Int position)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, ChunkWidth, (chunk, pos) => chunk.PopItem(pos));
    }

    #region Burst
    /*
    NativeHashMap<float2, Chunk> nativeChunks = new NativeHashMap<float2, Chunk>();
    public static float GetMovementSpeed(float2 position)
    {
        if (!TryGetBlock(position, out var block)) return 0;
        return block.MovementSpeed;
    }
    public static bool TryGetBlock(float2 position, out BlockSlice block)
    {
        block = default;
        if (Manager == null) return false;
        return Manager.ActiveRealm.TryGetBlock(position, ChunkWidth, out block);
    }
    */
    #endregion


    private void OnDestroy()
    {
        AllTaskShutdown.Cancel();
        foreach (var realm in Realms)
        {
            realm.Cleanup();
        }
    }

    private void OnDrawGizmos()
    {
        if (debug)
        {
            
        }
    }
}
