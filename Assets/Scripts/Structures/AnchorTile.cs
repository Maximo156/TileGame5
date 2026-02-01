using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum AnchorDirection
{
    Up,
    Right,
    Down,
    Left,
    None
}

[CreateAssetMenu(fileName = "NewAnchorTile", menuName = "Tile/AnchorTile", order = 1)]
public class AnchorTile : TileBase, ISpriteful
{
    public Sprite[] sprites;

    public AnchorDirection direction;

    [Min(0)]
    public int key;

    public bool Lock;

    private void OnValidate()
    {
        if(direction == AnchorDirection.None) direction = AnchorDirection.Up;
    }

    public Sprite Sprite
    {
        get {
            var index = (int)direction + (Lock ? 4 : 0);
            return index < sprites.Length ? sprites[index] : null;
        }
    }

    public Color Color => Utilities.ColorFromInt(key);

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = Sprite;
        tileData.color = Color;
        tileData.flags = TileFlags.LockColor;
    }
}
