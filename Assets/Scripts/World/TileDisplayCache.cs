using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class TileDisplayCache
{
    public Vector3Int[] WaterPositions { get; }
    public Vector3Int[] StonePositions { get; }
    public Dictionary<Vector3Int, int> RoofPositions { get; }

    public Dictionary<Vector3Int, int> DarknessPositions { get; }

    public (Vector3Int[] positions, TileBase[] tiles) GroundTiles { get; }
    public (Vector3Int[] positions, TileBase[] tiles) WallTiles { get; }

    public (Vector2Int[] positions, ItemStack[][] Items) PlacedItems { get; }

    public TileDisplayCache(BlockSlice[,] blocks, Vector2Int chunkWorldPos)
    {
        var water = new List<Vector3Int>();
        var stone = new List<Vector3Int>();
        var roof = new Dictionary<Vector3Int, int>();
        var darkness = new Dictionary<Vector3Int, int>();

        var groundPos = new List<Vector3Int>();
        var groundTile = new List<TileBase>();
        var wallPos = new List<Vector3Int>();
        var wallTile = new List<TileBase>();
        var itemPos = new List<Vector2Int>();
        var items = new List<ItemStack[]>();
        for (int x = 0; x < blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(0); y++)
            {
                var slice = blocks[x, y];
                var pos = new Vector3Int(x, y) + chunkWorldPos.ToVector3Int();
                if (slice.Water)
                {
                    water.Add(pos);
                }
                else
                {
                    stone.Add(pos);
                }
                if(slice.WallBlock is not null)
                {
                    wallPos.Add(pos);
                    wallTile.Add(slice.WallBlock.Display);
                }
                if (slice.GroundBlock is not null)
                {
                    groundPos.Add(pos);
                    groundTile.Add(slice.GroundBlock.Display);
                }
                if(slice.RoofBlock is not null)
                {
                    roof.Add(pos, slice.LightLevel);
                }
                darkness.Add(pos, slice.LightLevel);
                if (slice.PlacedItems is not null && slice.PlacedItems.Count > 0)
                {
                    itemPos.Add(pos.ToVector2Int());
                    items.Add(slice.PlacedItems.ToArray());
                }
            }
        }

        WaterPositions = water.ToArray();
        StonePositions = stone.ToArray();
        RoofPositions = roof;
        DarknessPositions = darkness;

        GroundTiles = (groundPos.ToArray(), groundTile.ToArray());
        WallTiles = (wallPos.ToArray(), wallTile.ToArray());
        PlacedItems = (itemPos.ToArray(), items.ToArray());
    }
}
