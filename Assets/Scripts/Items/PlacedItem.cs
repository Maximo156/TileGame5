using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlacedItem : MonoBehaviour
{
    public SpriteRenderer sprite;
    public TextMeshPro count;

    public void Render(ItemStack item)
    {
        sprite.sprite = item.GetSprite();
        count.text = item.GetString();
    }
}
