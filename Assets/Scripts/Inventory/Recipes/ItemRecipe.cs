using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemRecipe : Recipe
{
    public List<ItemStack> RequiredItems;
    public override List<ItemStack> Required => RequiredItems;

    public ItemStack Result;
    public int craftingTime;

    public int craftingTimeTicks => (craftingTime * 1000);// / WorldSettings.StructureTickMilliseconds;

    public override Sprite GetSprite() => Result.GetSprite();

    public override string GetString() =>  Result.GetString();

    public override (string, string) GetTooltipString() => Result.GetTooltipString();

    public override Color GetColor() => Result.GetColor();
}
