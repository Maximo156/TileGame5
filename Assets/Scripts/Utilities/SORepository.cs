using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SORepository
{
    public static Dictionary<string, Item> items;
    public static Dictionary<string, Block> blocks;

    static SORepository()
    {
        items = Resources.FindObjectsOfTypeAll<Item>().ToDictionary(i => i.name, i => i);
        blocks = Resources.FindObjectsOfTypeAll<Block>().ToDictionary(i => i.name, i => i);
    }
}
