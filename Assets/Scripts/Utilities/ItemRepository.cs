using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemRepository : MonoBehaviour 
{
    static Dictionary<string, Item> items;


    private void Awake()
    {
        items = Resources.FindObjectsOfTypeAll<Item>().ToDictionary(i => i.name, i => i);
    }

    public static Item GetItem(string name)
    {
        return items[name];
    }
}
