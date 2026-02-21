using UnityEngine;

public class PlayerSaver : MonoBehaviour
{
    PlayerInventories playerInventories;
    PlayerRespawner respawner;

    const string infoLocation = "player/info";
    const string invLocation = "player/inventory";
    private void Start()
    {
        playerInventories = GetComponent<PlayerInventories>();
        respawner = GetComponent<PlayerRespawner>();
        var info = LoadPlayerInfo();
        transform.position = info.Position.ToVector3Int();
        respawner.SpawnPoint = info.SpawnPoint;
        respawner.SpawnRealm = info.SpawnRealm;
        InitInventory();
    }

    private void OnDisable()
    {
        WorldSave.SaveSimple(infoLocation, 
            new PlayerInfo() 
            { 
                Position = Utilities.GetBlockPos(transform.position.ToVector2()),
                currentRealm = ChunkManager.CurRealm.name,
                SpawnPoint = respawner.SpawnPoint,
                SpawnRealm = respawner.SpawnRealm,
            }
        );;
        SaveInventory(null);
    }

    void InitInventory()
    {
        LoadInventory();
    }

    void SaveInventory(Inventory _)
    {
        WorldSave.SaveSimple(invLocation, new InventorySave()
        {
            Main = playerInventories.MainInv,
            Hotbar = playerInventories.HotbarInv,
            Accessory = playerInventories.AccessoryInv,
        });
    }

    void LoadInventory()
    {
        var loaded = WorldSave.LoadSimple<InventorySave>(invLocation);

        if (loaded != null) 
        {
            loaded.Main.CopyToInventory(playerInventories.MainInv);
            loaded.Hotbar.CopyToInventory(playerInventories.HotbarInv);
            loaded.Accessory.CopyToInventory(playerInventories.AccessoryInv);
        }
        else
        {
            playerInventories.HotbarInv.AddItems(WorldConfig.StartingHotbar);
        }
    }

    public static PlayerInfo LoadPlayerInfo()
    {
        return WorldSave.LoadSimple<PlayerInfo>(infoLocation);
    }

    public struct PlayerInfo
    {
        public Vector2Int Position;
        public Vector2Int SpawnPoint;
        public string SpawnRealm;
        public string currentRealm;
    }

    class InventorySave
    {
        public Inventory Main;
        public Inventory Hotbar;
        public Inventory Accessory;
    }
}
