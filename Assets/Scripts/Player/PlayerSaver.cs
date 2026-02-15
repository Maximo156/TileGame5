using UnityEngine;

public class PlayerSaver : MonoBehaviour
{
    PlayerInventories playerInventories;

    const string infoLocation = "player/info";
    const string invLocation = "player/inventory";
    private void Start()
    {
        playerInventories = GetComponent<PlayerInventories>();
        bool persist = WorldSave.ActiveSave.persistPlayer;
        if (persist)
        {
            var info = WorldSave.LoadSimple<PlayerInfo>(infoLocation);
            transform.position = info.Position.ToVector3Int();
        }
        InitInventory(persist);
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

    void InitInventory(bool persist)
    {
        if (persist)
        {
            LoadInventory();
        }
        else
        {
            playerInventories.MainInv.AddItems(playerInventories.MainStartingItems);
            playerInventories.HotbarInv.AddItems(playerInventories.HotbarStartingItems);
        }
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
            loaded.Main.TransferToInventory(playerInventories.MainInv);
            loaded.Hotbar.TransferToInventory(playerInventories.HotbarInv);
            loaded.Accessory.TransferToInventory(playerInventories.AccessoryInv);
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
