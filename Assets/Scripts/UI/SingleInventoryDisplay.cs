using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SingleInventoryDisplay : MonoBehaviour
{
    public ItemDrag ItemDrag;
    public SingleChildLayoutController display;
    Action<int, IGridItem, PointerEventData> onClickOverride;
    protected Action<int, IGridItem, PointerEventData> genOnClick(Inventory inv)
    {
        return (index, _, clickEvent) =>
        {
            ItemDrag.OnClick(inv, index, clickEvent);
        };
    }

    public void AttachInv(Inventory inv, Action<int, IGridItem, PointerEventData> onClickOverride = null)
    {
        this.onClickOverride = onClickOverride;
        inv.OnItemChanged += OnInventoryChanged;
        display.Render(inv, onClickOverride ?? genOnClick(inv));
    }

    public void DetachInv(Inventory inv)
    {
        if (inv != null)
        {
            inv.OnItemChanged -= OnInventoryChanged;
        }
    }

    public void Clear()
    {
        display.Clear();
    }

    private void OnInventoryChanged(Inventory inv)
    {
        display.Render(inv, onClickOverride ?? genOnClick(inv));
    }
}
