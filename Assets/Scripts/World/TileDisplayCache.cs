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

    public TileBase[] GroundTiles { get; }
    public TileBase[] WallTiles { get; }

    public (Vector2Int[] positions, ItemStack[][] Items) PlacedItems { get; }

    public Vector2Int chunkWorldPos { get; private set; }

    public TileDisplayCache(Dictionary<Vector2Int, BlockItemStack> placedItems, ChunkData data, Vector2Int chunkWorldPos)
    {
        this.chunkWorldPos = chunkWorldPos;
        var chunkWidth = data.chunkWidth;
        var chunkLength = chunkWidth * chunkWidth;
        var water = new List<Vector3Int>();
        var stone = new List<Vector3Int>();
        var roof = new Dictionary<Vector3Int, int>();
        var darkness = new Dictionary<Vector3Int, int>();

        var groundTile = new TileBase[chunkLength];
        var wallTile = new TileBase[chunkLength];

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

                wallTile.SetElement2d(y, x, chunkWidth, slice.wallBlock != 0 ? BlockDataRepo.GetBlock<Block>(slice.wallBlock).Display : null);
                groundTile.SetElement2d(y, x, chunkWidth, slice.groundBlock != 0 ? BlockDataRepo.GetBlock<Block>(slice.groundBlock).Display : null);

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

        GroundTiles = groundTile;
        WallTiles = wallTile;
        PlacedItems = (itemPos.ToArray(), items.ToArray());
    }
}
