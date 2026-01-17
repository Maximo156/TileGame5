using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

[CreateAssetMenu(fileName = "NewDoorTile", menuName = "Tile/DoorTile", order = 1)]
public class DoorTile : TileBase, ISpriteful {
    [Serializable]
    public class DoorOrientation
    {
        public Sprite Open;
        public Sprite Closed;

        public Sprite GetSprite(bool isOpen)
        {
            return isOpen ? Open : Closed;
        }
    }

    public DoorOrientation Horizontal = new();

    public DoorOrientation Vertical = new();

    public Color m_ColorOverride = Color.white;

    public Sprite Sprite => Horizontal.Closed;

    public Color Color => m_ColorOverride;

    public override void RefreshTile(Vector3Int location, ITilemap tilemap)
    {
        tilemap.RefreshTile(location);
        foreach (var v in Utilities.OctAdjacent3)
        {
            Vector3Int position = location + v;
            tilemap.RefreshTile(position);
        }
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        bool isOpen = false;
        if(ChunkManager.TryGetBlock(position.ToVector2Int(), out var slice))
        {
            isOpen = slice.simpleBlockState == 1;
        }
        tileData.colliderType = isOpen ? Tile.ColliderType.None : Tile.ColliderType.Sprite;
        tileData.color = m_ColorOverride;
        tileData.sprite = GetSprite(IsVertical(position, tilemap), isOpen);
    }

    private bool IsVertical(Vector3Int position, ITilemap tilemap)
    {
        return tilemap.GetTile(position + Vector3Int.up) is not null || tilemap.GetTile(position - Vector3Int.up) is not null;
    }

    private Sprite GetSprite(bool isVertical, bool isOpen)
    {
        return isVertical ? Vertical.GetSprite(isOpen) : Horizontal.GetSprite(isOpen);
    }
}