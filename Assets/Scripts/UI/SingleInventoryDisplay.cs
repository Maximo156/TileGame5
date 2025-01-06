using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SingleInventoryDisplay : MonoBehaviour
{
    public ItemDrag ItemDrag;
    public SingleChildLayoutController display;

    protected Action<int, IGridItem, PointerEventData> genOnClick(Inventory inv)
    {
        return (index, _, clickEvent) =>
        {
            ItemDrag.OnClick(inv, index, clickEvent);
        };
    }

    public void AttachInv(Inventory inv)
    {
        inv.OnItemChanged += OnInventoryChanged;
        display.Render(inv, genOnClick(inv));
    }

    public void DetachInv(Inventory inv)
    {
        if (inv != null)
        {
            inv.OnItemChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged(Inventory inv)
    {
        display.Render(inv, genOnClick(inv));
    }
}
