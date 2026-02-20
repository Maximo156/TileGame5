using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using NativeRealm;
using BlockDataRepos;
using UnityEngine.SceneManagement;

public class ChunkManager : MonoBehaviour
{
    public delegate void RealmChange(Realm oldRealm, Realm newRealm);
    public static event RealmChange OnRealmChange;

    static ChunkManager Manager;

    public static Realm CurRealm => Manager.ActiveRealm;
    public static string defaultRealm => Manager.startingRealm;

    [Header("Realm Settings")]
    public string startingRealm;
    public List<Realm> Realms;
    public GameObject EntityContainerPrefab;

    [Header("Chunk Settings")]
    public bool debug;

    Realm _activeRealm;
    Realm ActiveRealm
    {
        get => _activeRealm;
        set {
            _activeRealm?.Disable();
            OnRealmChange?.Invoke(_activeRealm, value);
            _activeRealm = value;
            _activeRealm.RefreshAllChunks();
            _activeRealm.Enable();
        }
    }

    public void Awake()
    { 
        Manager = this;
        PlayerMovement.OnPlayerChangedChunks += PlayerChangedChunks;
        PortalBlock.OnPortalBlockUsed += PortalUsed; 
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach(var realm in Realms)
        {
            realm.Initialize(EntityContainerPrefab, transform);
            realm.Disable();
        }
        var playerRealm = PlayerSaver.LoadPlayerInfo().currentRealm;
        if(string.IsNullOrWhiteSpace(playerRealm) )
        {
            playerRealm = startingRealm;
        }
        ActiveRealm = Realms.First(r => r.name == playerRealm);
        Task.Run(() => ChunkTick());
    }

    Realm frameRealm;
    private void Update()
    {
        frameRealm = ActiveRealm;
        if (frameRealm != null)
        {
            frameRealm.Step(currentChunk);
        }
    }

    private void LateUpdate()
    {
        if (frameRealm != null)
        {
            frameRealm.LateStep();
        }
    }

    private CancellationTokenSource AllTaskShutdown = new CancellationTokenSource();

    Vector2Int currentChunk;
    private void PlayerChangedChunks(Vector2Int curChunk)
    {
        currentChunk = curChunk;
        ActiveRealm.PlayerChangedChunks(currentChunk, AllTaskShutdown.Token);
    }

    public async void ChunkTick()
    {
        while (true && !AllTaskShutdown.Token.IsCancellationRequested)
        {
            await ActiveRealm.ChunkManagedTick(currentChunk, AllTaskShutdown.Token);
        }
        Debug.Log("Stopping Tick");
    }

    public static bool TryGetBlock(Vector2Int position, out NativeBlockSlice block, bool useProxy = true)
    {
        block = default;
        if (Manager == null) return false;
        return Manager.ActiveRealm.TryGetBlock(position, out block, useProxy);
    }

    public static bool TryGetBlockAndState(Vector2Int position, out NativeBlockSlice block, out BlockState state, bool useProxy = true)
    {
        block = default;
        state = null;
        if (Manager == null) return false;
        return Manager.ActiveRealm.TryGetBlockAndState(position, out block, out state, useProxy);
    }

    public static NativeBlockSlice GetBlock(Vector2Int position)
    {
        if(!TryGetBlock(position, out var block))
        {
            throw new InvalidOperationException("Checking ungenerated chunk");
        }
        return block;
    }

    private void PortalUsed(string newDim, PortalBlock exitBlock, Vector2Int worldPos)
    {
        SetActiveRealmImpl(newDim);
        var chunk = Utilities.GetChunk(worldPos, WorldConfig.ChunkWidth);
        CallbackManager.AddCallback(PlacePortal);

        void PlacePortal()
        {
            if(!ActiveRealm.TryGetChunk(chunk, out var c))
            {
                CallbackManager.AddCallback(PlacePortal);
                return;
            }
            CallbackManager.AddCallback(() => PlaceBlock(worldPos, Vector2Int.zero, exitBlock, true));
        }
    }

    void SetActiveRealmImpl(string newDim)
    {
        ActiveRealm = Realms.First(r => r.name == newDim);
        ActiveRealm.PlayerChangedChunks(currentChunk, AllTaskShutdown.Token);
    }

    public static void SetActiveRealm(string newDim)
    {
        Manager.SetActiveRealmImpl(newDim);
    }

    public static float GetMovementSpeed(Vector2Int position)
    {
        if (!TryGetBlock(position, out var block)) return 0;
        return block.GetMovementInfo().movementSpeed;
    }

    public static bool TryGetChunk(Vector2Int chunk, out Chunk chunkObj)
    {
        return Manager.ActiveRealm.TryGetChunk(chunk, out chunkObj);
    }

    public static bool PlaceItem(Vector2Int position, ItemStack item)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, (chunk, pos) => chunk.PlaceItem(pos, item));
    }

    public static ItemStack PopItem(Vector2Int position)
    {
        return Manager.ActiveRealm.PerformChunkAction(position, (chunk, pos) => chunk.PopItem(pos));
    }

    #region write actions
    public static bool PlaceBlock(Vector2Int position, Vector2Int dir, Block block, bool force = false, byte initialState = 0)
    {
        return Manager.ActiveRealm.QueueChunkAction(
            position, 
            (chunk, pos) => chunk.PlaceBlock(pos, dir, block, force, initialState),
            (chunk, pos) => chunk.CanPlace(pos, block.Id)
        );
    }

    public static bool Interact(Vector2Int position, InteractorInfo interactor)
    {
        return Manager.ActiveRealm.QueueChunkAction(
            position, 
            (chunk, pos) => chunk.Interact(pos, interactor),
            (chunk, pos) => BlockDataRepo.TryGetBlock<Wall>(chunk.GetBlock(pos).wallBlock, out var b) && b is IInteractableBlock
        );
    }

    public static void BreakBlock(Vector2Int position, bool roof, bool drop = true, bool useProxy = true)
    {
        Manager.ActiveRealm.QueueChunkAction(position, (chunk, pos) => chunk.BreakBlock(pos, roof, drop), useProxy);
    }
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
        if (Application.isPlaying && debug)
        {
            _activeRealm.DrawDebug();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneUnloaded += ResetEvent;
    }

    static void ResetEvent(Scene _)
    {
        Debug.Log("Resetting realm");
        OnRealmChange = null;
    }
}
