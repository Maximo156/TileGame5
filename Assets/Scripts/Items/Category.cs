using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class Category : IGridItem, IGridSource
{
    public string name;
    public Sprite sprite;
    public Color color = Color.white;

    [Header("Recipes")]
    public List<BlockRecipe> recipes;

    public IEnumerable<IGridItem> GetGridItems() => recipes;

    public Sprite GetSprite() => sprite;

    public string GetString() => "";

    public Color GetColor() => color;

    public (string, string) GetTooltipString() => (name, "");
}
