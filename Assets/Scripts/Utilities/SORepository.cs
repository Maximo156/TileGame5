using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SORepository
{
    public static Dictionary<string, Item> items;

    static SORepository()
    {
        items = Resources.FindObjectsOfTypeAll<Item>().ToDictionary(i => i.name, i => i);
    }
}
