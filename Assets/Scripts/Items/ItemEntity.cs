using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemEntity : MonoBehaviour
{
    public SpriteRenderer sr;
    public Rigidbody2D rb;
    public ItemStack stack { get; set; }
}
