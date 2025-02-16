using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class InfusionBlockDisplay : InteractiveDislay
{
    public SingleInventoryDisplay WeaponSlotDisplay;
    Inventory weaponSlot;

    public SingleInventoryDisplay WeaponInventoryDisplay;
    InfusableState weaponState;
    Inventory weaponInventory => weaponState?.Inventory.inv;

    public ItemDrag ItemDrag;

    private void Start()
    {
        weaponSlot = new Inventory(1);
        weaponSlot.OnItemChanged += InvChanged;
        WeaponSlotDisplay.AttachInv(weaponSlot);
        WeaponInventoryDisplay.Clear();
    }

    public override void Detach()
    {
        DetachWeaponInv();
        if (WorldPos != null) {
            Utilities.DropItems(WorldPos.Value, weaponSlot.GetAllItems(false));
            weaponSlot.RemoveItemIndex(0);
        }
        WorldPos = null;
        connectedBlock = null;
    }

    Vector2Int? WorldPos;
    InfusionBlock connectedBlock;
    public override void DisplayInventory(Vector2Int worldPos, BlockSlice slice, IInventoryContainer otherInventory)
    {
        WorldPos = worldPos;
        connectedBlock = slice.WallBlock as InfusionBlock;
    }

    public override Type TypeMatch()
    {
        return typeof(InfusionBlock);
    }

    List<int> usedSlots;
    void InvChanged(Inventory _)
    {
        DetachWeaponInv();
        var item = weaponSlot.CheckSlot(0);
        if(item is null || item.State is not InfusableState infusableState)
        {
            return;
        }
        weaponState = infusableState;
        WeaponInventoryDisplay.AttachInv(weaponInventory, OnWeaponInventoryClick);
        CalcUsedSlots();
    }

    void DetachWeaponInv()
    {
        if (weaponInventory is not null)
        {
            if (WorldPos != null)
            {
                var toDrop = weaponInventory.GetAllItems().Where((item, index) => item is not null && !usedSlots.Contains(index));
                Utilities.DropItems(WorldPos.Value, toDrop);
                for(int i = 0; i < weaponInventory.Count; i++)
                {
                    if (!usedSlots.Contains(i))
                    {
                        weaponInventory.RemoveItemIndex(i);
                    }
                }
            }
            WeaponInventoryDisplay.Clear();
            WeaponInventoryDisplay.DetachInv(weaponInventory);
            weaponState = null;
        }
    }

    void OnWeaponInventoryClick(int index, IGridItem clicked, PointerEventData eventData)
    {
        if((ItemDrag.currentlyHolding is null || (ItemDrag.currentlyHolding?.Item is ItemCharm charm && connectedBlock.AllowedCharms.Contains(charm))) && !usedSlots.Contains(index))
        {
            ItemDrag.OnClick(weaponInventory, index, eventData);
        }
    }

    public void CalcUsedSlots()
    {
        if (!weaponState.Validate())
        {
            throw new InvalidOperationException("Can't fuse, invalid state");
        }
        weaponState.UpdateStages();
        usedSlots = weaponInventory.GetAllItems().Select((item, index) => (item, index)).Where(t => t.item is not null).Select(t => t.index).ToList();
    }

    public void CleanItem()
    {
        if(weaponInventory is not null)
        {
            weaponInventory.RemoveItems(weaponInventory.GetAllItems(false));
            CalcUsedSlots();
        }
    }

    public void FractureItem()
    {
        if (weaponInventory is null || WorldPos is null) return;
        var chance = weaponSlot.CheckSlot(0)?.State is IDurableState durable ? durable.Durability.CurDurability * 1f / durable.Durability.MaxDurability : 1;
        var toDrop = weaponInventory.GetAllItems(false).Where(i => UnityEngine.Random.Range(0, 1f) < chance);
        Utilities.DropItems(WorldPos.Value, toDrop);
        weaponSlot.RemoveItemIndex(0);
    }
}
