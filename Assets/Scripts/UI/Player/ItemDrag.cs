using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemDrag : MonoBehaviour
{
    GridItemDisplay gridItem;
    public ItemStack currentlyHolding { get; set; }

    private void Start()
    {
        gridItem = GetComponent<GridItemDisplay>();
        gridItem.SetDisplay(currentlyHolding, -1);
    }

    public void OnClick(Inventory inventory, int index, PointerEventData clickEvent)
    {
        using var transaction = inventory.GetTransaction();
        var inSlot = inventory.CheckSlot(index);
        if(currentlyHolding == null)
        {
            currentlyHolding = inventory.RemoveItemIndex(index);
            if (clickEvent.button == PointerEventData.InputButton.Right && currentlyHolding != null && currentlyHolding.Count > 1)
            {
                var total = currentlyHolding.Count;
                currentlyHolding.Split(currentlyHolding.Count - currentlyHolding.Count / 2, out var toAdd);
                inventory.AddItemIndex(toAdd, index);
            }
        }
        else if(inSlot == null || inSlot.Item == currentlyHolding.Item)
        {
            if (clickEvent.button == PointerEventData.InputButton.Left || currentlyHolding.Count == 1)
            {
                if(inventory.AddItemIndex(currentlyHolding, index))
                {
                    currentlyHolding = null;
                }
            }
            else
            {
                currentlyHolding.Split(1, out var toAdd);
                if(!inventory.AddItemIndex(toAdd, index))
                {
                    currentlyHolding.Combine(toAdd);
                }
                if (currentlyHolding.Count == 0) currentlyHolding = null;
            }
        }
        else
        {
            inSlot = inventory.RemoveItemIndex(index);
            if(inventory.AddItemIndex(currentlyHolding, index))
            {
                currentlyHolding = inSlot;
            }
            else
            {
                inventory.AddItemIndex(inSlot, index);
            }
        }

        gridItem.SetDisplay(currentlyHolding, -1);
    }

    public void ExternalUpdate()
    {
        gridItem.SetDisplay(currentlyHolding, -1);
    }

    public void OnMouseMove(InputAction.CallbackContext value)
    {
        transform.position = value.ReadValue<Vector2>().ToVector3();
    }
}
