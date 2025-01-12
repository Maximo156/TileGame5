using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInventoryDisplay : MonoBehaviour
{
    public PlayerInventories inventory;

    public SingleInventoryDisplay HotBarDisplay;
    public HotBarSelector HotBarSelector;

    public SingleInventoryDisplay MainInventoryDisplay;

    public SingleInventoryDisplay AccessoryInventoryDisplay;

    public GameObject TogglableInv;

    public InteractiveDisplayController OtherDisplay;

    public void Start()
    {
        HotBarSelector.NewSelection(null, 0);
        PlayerInventories.OnHotBarChanged += HotBarSelector.NewSelection;
        PlayerMouseInput.OnBlockInterfaced += BlockInterfaced;

        HotBarDisplay.AttachInv(inventory.HotbarInv);
        MainInventoryDisplay.AttachInv(inventory.MainInv);
        AccessoryInventoryDisplay.AttachInv(inventory.AccessoryInv);

        TogglableInv.SetActive(false);
    }

    public void OnInvToggle()
    {
        TogglableInv.SetActive(!TogglableInv.activeSelf);
        if (!TogglableInv.activeSelf)
        {
            OtherDisplay.Close();
        }
    }

    private void BlockInterfaced(Vector2Int pos, BlockSlice slice, IInventoryContainer inv)
    {
        TogglableInv.SetActive(true);
    }
}
