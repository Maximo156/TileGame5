using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockRecipe : Recipe
{
    public Block block;
    public override List<ItemStack> Required => block.Drops;

    public override Color GetColor() => block.Color;

    public override Sprite Sprite => block.Sprite;

    public override string GetString() => "";

    public override (string, string) GetTooltipString() => (block.GetDisplayName(), "");
}
