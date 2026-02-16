using UnityEngine;

public class PlayerSaver : MonoBehaviour
{
    PlayerInventories playerInventories;

    const string infoLocation = "player/info";
    const string invLocation = "player/inventory";
    private void Start()
    {
        playerInventories = GetComponent<PlayerInventories>();
        var info = WorldSave.LoadSimple<PlayerInfo>(infoLocation);
        transform.position = info.Position.ToVector3Int();
        InitInventory();
    }

    private void OnDisable()
    {
        WorldSave.SaveSimple(infoLocation, 
            new PlayerInfo() 
            { 
                Position = Utilities.GetBlockPos(transform.position.ToVector2())
            }
        );
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

    struct PlayerInfo
    {
        public Vector2Int Position;
    }

    class InventorySave
    {
        public Inventory Main;
        public Inventory Hotbar;
        public Inventory Accessory;
    }
}
