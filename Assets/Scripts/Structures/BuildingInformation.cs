using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Tilemaps;
using System.Linq;
using Unity.Collections;
using NativeRealm;

public class BuildingInformation : MonoBehaviour
{
    [Serializable]
    public class LootTableEntry 
    {
        public float chance;
        public int min;
        public int max;
        public Item Item;
        public List<ItemStack> ItemFill;
    }

    public Tilemap Ground;
    public Tilemap Walls;
    public Tilemap Roof;
    Tilemap AnchorTiles;
    public List<LootTableEntry> lootTable = new List<LootTableEntry>();
    public int Importance;
    public bool AllowMountains;
    public Dictionary<int, List<AnchorInfo>> Anchors = new Dictionary<int, List<AnchorInfo>>();

    public BoundsInt Bounds { get; private set; }
    AnchorInfo[,] AnchorBlocks;
    BuildingBlockSlice[,] Slices;

    public BuildingBlockSlice GetSlice(Vector2Int pos)
    {
        return Slices[pos.x, pos.y];
    }

    public IEnumerable<(Vector2Int pos, AnchorInfo anchor)> GetAnchors(System.Random rand)
    {
        List<(Vector2Int pos, AnchorInfo anchor)> res = new();
        for(int x = 0; x < AnchorBlocks.GetLength(0); x++)
        {
            for (int y = 0; y < AnchorBlocks.GetLength(1); y++)
            {
                if(AnchorBlocks[x,y] != null)
                {
                    res.Add((new Vector2Int(x, y), AnchorBlocks[x, y]));
                }
            }
        }
        return res.Shuffle(rand);
    }

    public List<AnchorInfo> GetOpenAnchor(int key)
    {
        if (Anchors.TryGetValue(key, out var anchors))
        {
            return anchors.Where(a => a.Lock).ToList();
        }
        return new List<AnchorInfo>();
    }

    public bool HasAnchor(int key)
    {
        return Anchors.TryGetValue(key, out var anchors) && anchors.Any(a => a.Lock);
    }

    public List<ItemStack> GenerateLootEntry(System.Random rand)
    {
        var res = new List<ItemStack>();
        foreach (var entry in lootTable)
        {
            if(rand.NextDouble() < entry.chance)
            {
                var item = new ItemStack(entry.Item, rand.Next(entry.min, entry.max));
                if(item.GetState<ItemInventoryBehaviourState>(out var invState))
                {
                    foreach (var i in entry.ItemFill) {
                        invState.inv.AddItem(i);
                    }
                }
                res.Add(item);
            }
        }
        return res;
    }

    public BoundsInt ComputeBounds()
    {
        Ground.CompressBounds();
        Walls.CompressBounds();
        Roof.CompressBounds();
        AnchorTiles.CompressBounds();

        var bounds = new BoundsInt();
        var min = Vector3Int.Min(Vector3Int.Min(Ground.cellBounds.min, Walls.cellBounds.min), Vector3Int.Min(Roof.cellBounds.min, AnchorTiles.cellBounds.min));
        var max = Vector3Int.Max(Vector3Int.Max(Ground.cellBounds.max, Walls.cellBounds.max), Vector3Int.Max(Roof.cellBounds.max, AnchorTiles.cellBounds.max));
        bounds.SetMinMax(min, max);

        if (Bounds.min != Vector3Int.zero) throw new InvalidOperationException($"Ensure min Bounds is 0, 0, 0. Current is: {Bounds.min}");

        return bounds;
    }

    public void InitializeNativeComponent(ref NativeStructureComponent targetComponent, NativeSlice<NativeComponentBlockSlice> blocks, NativeSlice<NativeComponentAnchor> anchors)
    {
        var bounds = targetComponent.Bounds;
        var ground = Ground.GetTilesBlock(Bounds).Select(GetBlock).ToArray();
        var walls = Walls.GetTilesBlock(Bounds).Select(GetBlock).ToArray();
        var roofs = Roof.GetTilesBlock(Bounds).Select(GetBlock).ToArray();

        var newAnchors = AnchorTiles.GetTilesBlock(Bounds).Select(ProcessAnchor).ToArray();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                var newSlice = new BuildingBlockSlice();
                newSlice.GroundBlock = ground[x + y * Bounds.size.x] as Ground;
                newSlice.WallBlock = walls[x + y * Bounds.size.x] as Wall;
                newSlice.RoofBlock = roofs[x + y * Bounds.size.x] as Roof;
                blocks.SetElement2d(x, y, bounds.size.x, new NativeComponentBlockSlice() 
                {
                    groundBlock = ground[x + y * Bounds.size.x]?.Id ?? 0,
                    wallBlock = walls[x + y * Bounds.size.x]?.Id ?? 0,
                    roofBlock = roofs[x + y * Bounds.size.x]?.Id ?? 0
                });

                if (newSlice.HasBlock())
                {
                    Slices[x, y] = newSlice;
                }

                anchors.SetElement2d(x, y, bounds.size.x, newAnchors[x + y * Bounds.size.x]);
            }
        }

        targetComponent.AllowMountains = AllowMountains;
        targetComponent.Importance = (int)MathF.Max(1, Importance);
    }

    private Block GetBlock(TileBase tile, int index)
    {
        if (tile is AnchorTile) throw new InvalidOperationException("Anchor tile found in floor or block layer");
        if (tile is BuildingTile building) 
        {
            return building.GetBlock() ?? throw new InvalidOperationException($"Block not found on {building.name}");
        }
        return null;
    }

    private NativeComponentAnchor ProcessAnchor(TileBase tile, int index)
    {
        if (tile is null) return new NativeComponentAnchor() { direction = AnchorDirection.None };
        if (tile is not AnchorTile anchorTile) throw new InvalidOperationException("Non-Anchor tile found in anchor layer");
        var x = index % Bounds.size.x;
        var y = index / Bounds.size.x;
        var anchorInfo = new NativeComponentAnchor()
        {
            direction = anchorTile.direction,
            offset = new Vector2Int(x, y),
            key = anchorTile.key,
            Lock = anchorTile.Lock
        };
        return anchorInfo;
    }
}

public class BuildingBlockSlice
{
    public Wall WallBlock;
    public Ground GroundBlock;
    public Roof RoofBlock;

    public BlockState State;

    public bool HasBlock() => WallBlock != null || GroundBlock != null || RoofBlock != null;

    public BuildingBlockSlice()
    {

    }

    public BuildingBlockSlice(BuildingBlockSlice reference)
    {
        WallBlock = reference?.WallBlock;
        GroundBlock = reference?.GroundBlock;
        RoofBlock = reference?.RoofBlock;
    }
}
