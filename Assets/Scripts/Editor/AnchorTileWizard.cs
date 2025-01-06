using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

public class GenAnchorTile : ScriptableWizard
{
    public int Key;

    [MenuItem("Assets/Create/AnchorTiles/Generate Anchor Tile Set...")]
    static void CreateWizard()
    {
        DisplayWizard("Generate Anchor Tile Set", typeof(GenAnchorTile));
    }

    private void OnWizardCreate()
    {
        var KeySprites = Resources.LoadAll<Sprite>($"Sprites/Anchors/Keys");
        var LockSprites = Resources.LoadAll<Sprite>($"Sprites/Anchors/Locks");
        var sprites = order.Select(i => KeySprites[i]).Concat(order.Select(i => LockSprites[Utilities.modulo(i-2, 4)])).ToArray();
        foreach (var dir in (AnchorDirection[])Enum.GetValues(typeof(AnchorDirection)))
        {
            var key = CreateInstance<AnchorTile>();
            var Lock = CreateInstance<AnchorTile>();

            key.direction = dir;
            Lock.direction = dir;

            key.key = Key;
            Lock.key = Key;

            Lock.Lock = true;

            key.sprites = sprites;
            Lock.sprites = sprites;

            AssetDatabase.CreateAsset(key, $"Assets/Prefabs/Buildings/AnchorTiles/{Key + dir.ToString()}Key.asset");
            AssetDatabase.CreateAsset(Lock, $"Assets/Prefabs/Buildings/AnchorTiles/{Key + dir.ToString()}Lock.asset");
        }
    }

    int[] order = new int[] { 1, 0, 3, 2 };
}
