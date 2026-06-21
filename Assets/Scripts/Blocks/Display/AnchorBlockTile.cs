using ComposableBlocks;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "AnchorBlockTile", menuName = "Tile/AnchorBlockTile", order = 1)]
public class AnchorBlockTile : TileBase, ISpriteful
{
    public Sprite[] sprites;

    public Sprite Sprite
    {
        get
        {
            if (sprites == null || sprites.Length == 0) 
            { 
                return null; 
            }
            return sprites[0];
        }
    }

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
        var code = 0;
        var key = true;
        var dir = AnchorDirection.Up;
        if (ChunkManager.TryGetBlock(position.ToVector2Int(), out var slice))
        {
            (code, key, dir) = AnchorBlockBehaviour.DecodeState(slice.simpleBlockState);
        }

        var index = (int)dir + (!key ? 4 : 0);
        var sprite = index < sprites.Length ? sprites[index] : null;

        tileData.sprite = sprite;
        tileData.color = Utilities.ColorFromInt(code);
        tileData.flags = TileFlags.LockColor;
    }
}
