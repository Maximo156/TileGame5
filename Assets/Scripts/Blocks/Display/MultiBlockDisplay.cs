using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewMultiTile", menuName = "Tile/MultiTile", order = 1)]
public class MultiBlockDisplay : TileBase, ISpriteful
{
    public Sprite Up;
    public Sprite Down;
    public Sprite Left;
    public Sprite Right;

    public Sprite Sprite => Up;

    public Color m_ColorOverride = Color.white;
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
        Vector2Int dir = Vector2Int.up;
        if (ChunkManager.TryGetBlock(position.ToVector2Int(), out var slice))
        {
            dir = Utilities.QuadAdjacent[slice.simpleBlockState];
        }
        tileData.colliderType = Tile.ColliderType.Sprite;
        tileData.color = m_ColorOverride;
        tileData.sprite = GetSprite(dir);
    }

    Sprite GetSprite(Vector2Int dir)
    {
        switch (dir)
        {
            case Vector2Int v when v.Equals(Vector2Int.left):
                return Left;
            case Vector2Int v when v.Equals(Vector2Int.down):
                return Down;
            case Vector2Int v when v.Equals(Vector2Int.right):
                return Right;
            default:
                return Up;
        }
    }
}
