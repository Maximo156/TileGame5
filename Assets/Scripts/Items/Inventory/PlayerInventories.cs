using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventories : MonoBehaviour, IInventoryContainer
{
    public delegate void HotBarChanged(ItemStack newInHand, int newPos);
    public static event HotBarChanged OnHotBarChanged;

    [Header("Main Inventory")]
    public List<ItemStack> MainStartingItems;
    public int MainSize;
    public Inventory MainInv { get; private set; }

    [Header("Hotbar Inventory")]
    public List<ItemStack> HotbarStartingItems;
    public int HotbarSize;
    private int CurrentlySelectedHand;

    [Header("Controls")]
    public ToolDisplay tool;
    public Inventory HotbarInv { get; private set; }

    public Inventory AccessoryInv { get; private set; }

    void Awake()
    {
        MainInv = new Inventory(MainSize, MainStartingItems);
        HotbarInv = new Inventory(HotbarSize, HotbarStartingItems);
        HotbarInv.OnItemChanged += HotBarItemsChanged;
        AccessoryInv = new AccessoryInv();
    }

    public void Start()
    {
        HotBarItemsChanged(HotbarInv);
    }

    public void Scroll(InputAction.CallbackContext value)
    {
        if (!Keyboard.current.ctrlKey.isPressed && !tool.animating)
        {
            var dir = value.ReadValue<Vector2>();

            if (dir.magnitude > 0)
            {
                CurrentlySelectedHand += (int)Mathf.Sign(dir.y);

                if (CurrentlySelectedHand < 0) CurrentlySelectedHand = HotbarSize - 1;
                else CurrentlySelectedHand %= HotbarSize;

                HotBarItemsChanged(HotbarInv);
            }
        }
    }

    public void OnGetNumber(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            var input = (int)value.ReadValue<float>() % HotbarSize;
            CurrentlySelectedHand = input;
            HotBarItemsChanged(HotbarInv);
        }
    }

    ItemStack curInHand;
    private void HotBarItemsChanged(IInventory inv)
    {
        var InHand = inv.CheckSlot(CurrentlySelectedHand);
        if (InHand == null || InHand != curInHand)
        {
            curInHand = InHand;
            OnHotBarChanged?.Invoke(InHand, CurrentlySelectedHand);
        }
    }

    public IEnumerable<IInventory> GetIndividualInventories()
    {
        return new[] { MainInv, HotbarInv };
    }
}
