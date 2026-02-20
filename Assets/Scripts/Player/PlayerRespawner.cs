using BlockDataRepos;
using NativeRealm;
using System;
using System.Collections;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawner : MonoBehaviour
{
    public delegate void PlayerRespawn(PlayerRespawner spawner, Action callback);
    public static event PlayerRespawn OnPlayerRespawn;
    public Vector2Int SpawnPoint { get; set; }
    public string SpawnRealm { get; set; }

    public PlayerInventories PlayerInventories;

    public InputController InputController;
    public ItemPickup ItemPickup;
    public Collider2D ItemCollision;

    private void Start()
    {
        if (string.IsNullOrEmpty(SpawnRealm)) SpawnRealm = ChunkManager.defaultRealm;
    }

    public void Die()
    {
        SetDependenciesEnabled(false);
        Utilities.DropItems(Utilities.GetBlockPos(transform.position), (PlayerInventories as IInventoryContainer).RemoveAllItems().Where(i => i != null), 5*60);
        OnPlayerRespawn?.Invoke(this, RespawnFromDeath);
    }

    public void Sleep()
    {
        SetDependenciesEnabled(false);
        OnPlayerRespawn?.Invoke(this, DayTime.dayTime.SetMorning);
    }

    void RespawnFromDeath()
    {
        foreach (var item in WorldConfig.StartingHotbar)
        {
            PlayerInventories.HotbarInv.AddItem(new(item));
        }

        SendMessage("Respawn", SendMessageOptions.DontRequireReceiver);
    }

    public async Awaitable TriggerRespawn(Action callback)
    {
        callback();

        await ResolveSpawnPoint();

        SetDependenciesEnabled(true);
    }

    void SetDependenciesEnabled(bool enabled)
    {
        InputController.enabled = enabled;
        ItemPickup.enabled = enabled;
        ItemCollision.enabled = enabled;
    }

    async Awaitable ResolveSpawnPoint()
    {
        transform.position = SpawnPoint.ToVector3Int();
        if (SpawnPoint == Vector2Int.zero && SpawnRealm == ChunkManager.defaultRealm ) return;

        if(ChunkManager.CurRealm.name != SpawnRealm)
        {
            ChunkManager.SetActiveRealm(SpawnRealm);
        }
        NativeBlockSlice block;
        while(!ChunkManager.TryGetBlock(SpawnPoint, out block))
        {
            await Awaitable.NextFrameAsync();
        }
        if (BlockDataRepo.TryGetBlock<Bed>(block.wallBlock, out var _))
        {
            foreach(var v in Utilities.QuadAdjacent)
            {
                if(ChunkManager.TryGetBlock(SpawnPoint + v, out block) && block.wallBlock == 0)
                {
                    transform.position = (SpawnPoint + v).ToVector3Int();
                    return;
                }
            }
        }
        SpawnPoint = Vector2Int.zero;
        SpawnRealm = ChunkManager.defaultRealm;
        await ResolveSpawnPoint();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneUnloaded += ResetEvent;
    }

    static void ResetEvent(Scene _)
    {
        OnPlayerRespawn = null;
    }
}
