using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor.U2D.Sprites;
using UnityEngine.Tilemaps;

public class EightWayTileWizardTile : ScriptableWizard
{
    public string BlockName = "name";

    Color Target = new Color(1, 0, 1);

    [MenuItem("Assets/Create/EightWayTile/Generate Eight Way Tile Set...")]
    static void CreateWizard()
    {
        DisplayWizard("Generate Eight Way Tile Set", typeof(EightWayTileWizardTile));
    }

    private void OnWizardCreate()
    {
        var baseTile = Resources.Load<RuleTile>("Tiles/BASE8Way");
        var sprites = Resources.LoadAll<Sprite>($"Sprites/Blocks/{BlockName}Blocks");

        var newTile = CreateInstance<RuleOverrideTile>();
        newTile.m_Tile = baseTile;

        newTile.Override();
        var overrides = new List<KeyValuePair<Sprite, Sprite>>();
        var count = 0;
        newTile.GetOverrides(overrides, ref count);
        var newOverrides = overrides.Select((kvp) =>
            {
                var indexString = kvp.Key.name.Split("_")[^1];
                var index = int.Parse(indexString);
                return new KeyValuePair<Sprite, Sprite>(kvp.Key, sprites[index]);
            }
        ).ToList();
        newTile.ApplyOverrides(newOverrides);
        newTile.PrepareOverride();
        AssetDatabase.CreateAsset(newTile, $"Assets/ScriptableObjects/Blocks/Display/{BlockName}Tile.asset");
    }
}
