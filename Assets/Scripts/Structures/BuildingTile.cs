using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewBuildingTile", menuName = "Tile/BuildingTile", order = 1)]
public class BuildingTile : TileBase, ISpriteful
{
    public List<Block> Blocks;

    public Sprite Sprite => GetBlock()?.Sprite;

    public Color Color => GetBlock()?.Color ?? Color.white;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        var display = GetBlock()?.Display;
        if (display is not null) 
        { 
            display.GetTileData(position, tilemap, ref tileData);
        }
        tileData.sprite = Sprite;
        //tileData.color = Color;
    }

    public Block GetBlock()
    {
        return Blocks?.SelectRandom();
    }
}
