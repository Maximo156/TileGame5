using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CraftingDisplay : InteractiveDislay
{
    public SingleChildLayoutController CraftingGrid;
    public Image Completion;

    public override void Detach()
    {
        if(otherInv != null)
        {
            foreach (var inv in otherInv.GetIndividualInventories())
            {
                inv.OnItemChanged -= Render;
            }
        }
        block = null;
        otherInv=null; 
    }

    CraftingBlock block;
    IInventoryContainer otherInv;
    Vector2 worldPos;

    public override void DisplayInventory(Vector2Int worldPos, BlockSlice slice, IInventoryContainer otherInv)
    {
        Completion.fillAmount = 0;
        block = slice.WallBlock as CraftingBlock;
        this.otherInv = otherInv;
        this.worldPos = worldPos;
        foreach (var inv in otherInv.GetIndividualInventories())
        {
            inv.OnItemChanged += Render;
        }
        Render(null);
    }

    void Render(Inventory _)
    {
        CraftingGrid.Render(block, OnSlotDown, OnSlotUp, ItemDisplayOverride);
    }

    public override Type TypeMatch()
    {
        return typeof(CraftingBlock);
    }

    Coroutine craftingRoutine;

    public void OnSlotDown(int index, IGridItem item, PointerEventData eventData)
    {
        var recipe = item as ItemRecipe;
        if (CanCraft(recipe))
        {
            craftingRoutine = StartCoroutine(CraftItem(recipe));
        }
    }

    private bool CanCraft(ItemRecipe recipe)
    {
        var correctIngredients = recipe.CanProduce(otherInv.GetAllItems());
        return correctIngredients;
    }

    public void OnSlotUp(int index, IGridItem item, PointerEventData eventData)
    {
        if (craftingRoutine != null)
        {
            StopCoroutine(craftingRoutine);
        }
        Completion.fillAmount = 0;
    }

    private GridItemDisplay.CleanupOverride ItemDisplayOverride(IGridItem item, Image img, TextMeshProUGUI text, Slider slider)
    {
        if(item is not ItemRecipe recipe || CanCraft(recipe)) { return null; }
        var origColor = img.color;
        var color = origColor;

        color.a = 0.5f;
        img.color = color;

        return null;
    }

    public IEnumerator CraftItem(ItemRecipe recipe)
    {
        var targetTime = Time.time + recipe.craftingTime;
        while(Time.time < targetTime)
        {
            Completion.fillAmount = 1 - ((targetTime - Time.time) / recipe.craftingTime);
            yield return null;
        }
        recipe.CanProduce(otherInv.GetAllItems());
        var leftover = otherInv.AddItem(recipe.Result);
        if(leftover != null)
        {
            ItemEntityManager.SpawnItem(Vector2Int.FloorToInt(worldPos), leftover);
        }
        recipe.UseRecipe(otherInv);
        Completion.fillAmount = 0;
        if (CanCraft(recipe))
        {
            craftingRoutine = StartCoroutine(CraftItem(recipe));
        }
    }
}
