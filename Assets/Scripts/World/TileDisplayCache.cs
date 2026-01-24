using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using NativeRealm;
using Unity.Collections;
using BlockDataRepos;

public class TileDisplayCache
{
    public Vector3Int[] WaterPositions { get; }
    public Vector3Int[] StonePositions { get; }
    public Dictionary<Vector3Int, int> RoofPositions { get; }

    public Dictionary<Vector3Int, int> DarknessPositions { get; }

    public (Vector3Int[] positions, TileBase[] tiles) GroundTiles { get; }
    public (Vector3Int[] positions, TileBase[] tiles) WallTiles { get; }

    public (Vector2Int[] positions, ItemStack[][] Items) PlacedItems { get; }

    public TileDisplayCache(Dictionary<Vector2Int, BlockSliceState> placedItems, ChunkData data, Vector2Int chunkWorldPos)
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
        foreach(var kvp in placedItems)
        {
            var slice = kvp.Value;
            var pos = kvp.Key.ToVector3Int() + chunkWorldPos.ToVector3Int();
            if (slice?.placedItems is not null && slice.placedItems.Count > 0)
            {
                itemPos.Add(pos.ToVector2Int());
                items.Add(slice.placedItems.ToArray());
            }
        }

        for(int x = 0; x < data.chunkWidth; x++)
        {
            for(int y = 0; y < data.chunkWidth; y++)
            {
                var slice = data.GetSlice(x, y);
                var pos = new Vector3Int(x, y) + chunkWorldPos.ToVector3Int();
                if (slice.isWater)
                {
                    water.Add(pos);
                }
                else
                {
                    stone.Add(pos);
                }
                if (slice.wallBlock != 0)
                {
                    wallPos.Add(pos);
                    wallTile.Add(BlockDataRepo.GetBlock<Block>(slice.wallBlock).Display);
                }
                if (slice.groundBlock != 0)
                {
                    groundPos.Add(pos);
                    groundTile.Add(BlockDataRepo.GetBlock<Block>(slice.groundBlock).Display);
                }
                if (slice.roofBlock != 0)
                {
                    roof.Add(pos, slice.lightLevel);
                }
                darkness.Add(pos, slice.lightLevel);
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
