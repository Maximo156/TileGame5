using UnityEngine;
using EntityStatistics;
using TMPro;
using NativeRealm;
using ComposableBlocks;

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

        HotBarDisplay.AttachInv(inventory.HotbarInv);
        MainInventoryDisplay.AttachInv(inventory.MainInv);
        AccessoryInventoryDisplay.AttachInv(inventory.AccessoryInv);

        InventorySetActive(false);
        CreativeInventoryContainer.SetActive(false);
    }

    public void OnInvToggle()
    {
        if (!InputController.Instance.AllowMovement) return;
        InventorySetActive(!TogglableInv.activeSelf);
        if (!TogglableInv.activeSelf)
        {
            OtherDisplay.Close();
        }
        CreativeInventoryContainer.SetActive(false);
    }

    void OnStatChanged(EntityStats.Stat stat)
    {
        if(stat == EntityStats.Stat.Defense)
        {
            Defence.text = Stats.GetStat(EntityStats.Stat.Defense).ToString();
        }
    }

    public void InventorySetActive(bool active)
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
