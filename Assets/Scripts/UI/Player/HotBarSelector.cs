using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotBarSelector : MonoBehaviour
{
    List<Image> images = new List<Image>();

    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            images.Add(transform.GetChild(i).GetComponent<Image>());
        }
    }

    public void NewSelection(ItemStack _, int newPos)
    {
        for(int i = 0; i < images.Count; i++)
        {
            if(i == newPos)
            {
                images[i].enabled = true;
            }
            else
            {
                images[i].enabled = false;
            }
        }
    }
}
