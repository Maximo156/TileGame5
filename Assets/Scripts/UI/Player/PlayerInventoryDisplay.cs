using UnityEngine;
using EntityStatistics;
using TMPro;
using NativeRealm;

public class PlayerInventoryDisplay : MonoBehaviour
{
    public PlayerInventories inventory;
    EntityStats Stats;

    public SingleInventoryDisplay HotBarDisplay;
    public HotBarSelector HotBarSelector;

    public SingleInventoryDisplay MainInventoryDisplay;

    public SingleInventoryDisplay AccessoryInventoryDisplay;

    public GameObject TogglableInv;
    public GameObject ModifierDisplay;
    public TextMeshProUGUI Defence;

    public InteractiveDisplayController OtherDisplay;

    public GameObject CreativeInventoryContainer;

    public void Start()
    {
        HotBarSelector.NewSelection(null, 0);
        inventory.OnHotBarChanged += HotBarSelector.NewSelection;
        Stats = inventory.gameObject.GetComponent<EntityStats>();
        Stats.OnStatChanged += OnStatChanged;
        Defence.text = Stats.GetStat(EntityStats.Stat.Defense).ToString();
        PlayerMouseInput.OnBlockInterfaced += BlockInterfaced;

        HotBarDisplay.AttachInv(inventory.HotbarInv);
        MainInventoryDisplay.AttachInv(inventory.MainInv);
        AccessoryInventoryDisplay.AttachInv(inventory.AccessoryInv);

        InventorySetActive(false);
    }

    public void OnInvToggle()
    {
        InventorySetActive(!TogglableInv.activeSelf);
        if (!TogglableInv.activeSelf)
        {
            OtherDisplay.Close();
        }
        CreativeInventoryContainer.SetActive(false);
    }

    private void BlockInterfaced(Vector2Int pos, Wall _, BlockState state, IInventoryContainer inv)
    {
        InventorySetActive(true);
    }

    void OnStatChanged(EntityStats.Stat stat)
    {
        if(stat == EntityStats.Stat.Defense)
        {
            Defence.text = Stats.GetStat(EntityStats.Stat.Defense).ToString();
        }
    }

    private void InventorySetActive(bool active)
    {
        TogglableInv.SetActive(active);
        ModifierDisplay.SetActive(!active);
    }

    public void ToggleCreativeInv()
    {
        if (!GameSettings.CreativeMenu) return;

        CreativeInventoryContainer.SetActive(!CreativeInventoryContainer.activeSelf);
        InventorySetActive(true);
    }
}
