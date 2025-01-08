using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

public class Chunk
{
    public delegate void BlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, BlockSlice block);
    public static event BlockChanged OnBlockChanged;

    public delegate void BlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos);
    public static event BlockRefreshed OnBlockRefreshed;

    public delegate void LightingUpdated(Dictionary<Vector3Int, int> updated);
    public static event LightingUpdated OnLightingUpdated;

    public delegate void ChunkChanged(Chunk chunk);
    public static event ChunkChanged OnChunkChanged;

    public Vector2Int ChunkPos;
    public Vector2Int BlockPos => ChunkPos * width;

    public BlockSlice[,] blocks { get; private set; }
    readonly int width;
    System.Random rand;

    ChunkGenerator curGenerator;

    public Chunk(Vector2Int chunkPos, int width)
    {
        this.width = width;
        ChunkPos = chunkPos;
    }

    public async Task Generate(ChunkGenerator generator, Vector2Int? worldPos = null, Block portal = null)
    {
        curGenerator = generator;
        rand = new System.Random(ChunkPos.GetHashCode());
        blocks = await generator.GetBlockSlices(ChunkPos, BlockPos, width, rand);
        if(worldPos != null)
        {
            GetBlock(worldPos.Value).SetBlock(portal);
        }
        tileDisplayCache = new TileDisplayCache(blocks, BlockPos);
        CallbackManager.AddCallback(() =>
        {
            OnChunkChanged?.Invoke(this);
        });
    }

    public void ChunkTick(CancellationToken cancellationToken)
    {
        bool updated = false;
        List<(Vector2Int pos, BlockSlice slice)> updateInfo = new List<(Vector2Int pos, BlockSlice slice)>();
        for(int x = 0; x < blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(0); y++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var slice = blocks[x, y];
                var worldPos = new Vector2Int(x, y) + BlockPos;
                if (slice.Tick(worldPos, rand))
                {
                    updated |= true;
                    updateInfo.Add((worldPos, slice));
                }
            }
        }
        if (updated)
        {
            RegenCache();
            CallbackManager.AddCallback(() =>
            {
                foreach (var (pos, slice) in updateInfo)
                {
                    OnBlockChanged?.Invoke(this, pos, ChunkPos, slice);
                }
            });
        }
    }

    public BlockSlice GetBlock(Vector2Int world)
    {
        var localPos = WorldToLocal(world);
        return blocks[localPos.x, localPos.y];
    }

    public bool PlaceBlock(Vector2Int position, Block block, bool force = false)
    {
        var slice = GetBlock(position);
        var res = force ? slice.SetBlock(block) : slice.SafeSet(block);
        if (res)
        {
            if (block is Roof roof) {
                CalcRoofStrengthBFS(position, roof);
            }
            if(block is LightBlock)
            {
                CalcLight(position);
            }
            ChangedBlock(position, slice);
        }
        return res;
    }

    public bool BreakBlock(Vector2Int worldPosition, bool roof, bool drop = true)
    {
        var slice = GetBlock(worldPosition);

        if(slice.Break(worldPosition, roof, out var broken))
        {
            CalcRoofStrengthBFS(worldPosition, broken as Roof);
            if (broken is LightBlock)
            {
                CalcLight(worldPosition);
            }
        }

        ChangedBlock(worldPosition, slice);
        return true;
    }

    public bool PlaceItem(Vector2Int worldPosition, ItemStack item)
    {
        var slice = GetBlock(worldPosition);

        var res = slice.PlaceItem(item);
        ChangedBlock(worldPosition, slice);
        return res;
    }

    public ItemStack PopItem(Vector2Int worldPosition)
    {
        var slice = GetBlock(worldPosition);

        var res = slice.PopItem();
        ChangedBlock(worldPosition, slice);
        return res;
    }

    public bool Interact(Vector2Int worldPosition)
    {
        var slice = GetBlock(worldPosition);
        if(slice.WallBlock is IInteractableBlock interactable)
        {
            var oldDisplay = slice.WallBlock.Display;
            if (interactable.Interact(worldPosition, slice))
            {
                if (oldDisplay == slice.WallBlock.Display)
                {
                    RefreshBlock(worldPosition);
                }
                else
                {
                    ChangedBlock(worldPosition, slice);
                }
            }
            return true;
        }
        return false;
    }

    public void CalcRoofStrengthBFS(Vector2Int worldPosition, Roof curRoof)
    {
        ChunkManager.TryGetBlock(worldPosition, out var initialSlice);
        var roofStrength = curRoof?.Strength ?? initialSlice.RoofBlock.Strength;
        var radius = roofStrength * 2;
        var grid = new int[2 * radius + 1, 2 * radius + 1];
        Queue<Vector2Int> toCheck = new();
        for(int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var local = new Vector2Int(x, y);
                if (ChunkManager.TryGetBlock(localToWorld(local), out var s) && s.WallBlock?.structural == true && s.RoofBlock is not null)
                {
                    toCheck.Enqueue(local);
                    grid[x, y] = 0;
                }
                else
                {
                    grid[x, y] = int.MaxValue;
                }
            }
        }

        while(toCheck.Count > 0)
        {
            var cur = toCheck.Dequeue();
            foreach(var newPos in Utilities.QuadAdjacent.Select(v => v + cur))
            {
                if(0 <= newPos.x && newPos.x < radius * 2 - 1 && 0 <= newPos.y && newPos.y < radius * 2 - 1 && ChunkManager.TryGetBlock(localToWorld(newPos), out var s) && s.RoofBlock is not null)
                {
                    if(grid[newPos.x, newPos.y] > grid[cur.x, cur.y] + 1)
                    {
                        grid[newPos.x, newPos.y] = grid[cur.x, cur.y] + 1;
                        toCheck.Enqueue(newPos);
                    }
                }
            }
        }

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var worldpos = localToWorld(new Vector2Int(x, y));
                if (ChunkManager.TryGetBlock(worldpos, out var s) && s.RoofBlock is not null && s.RoofBlock.Strength < grid[x, y] && worldpos.ManhattanDistance(worldPosition) <= roofStrength)
                {
                    s.Break(worldpos, true, out var _);
                    OnBlockChanged?.Invoke(this, worldpos, ChunkPos, s);
                }
            }
        }

        Vector2Int localToWorld(Vector2Int local)
        {
            return worldPosition + local - Vector2Int.one * (radius - 1);
        }
    }

    public void CalcLight(Vector2Int worldPosition)
    {
        Queue<Vector2Int> toCheck = new();
        Dictionary<Vector3Int, int> updated = new();
        foreach (var v in Utilities.QuadAdjacent.Select(v => v + worldPosition))
        {
            toCheck.Enqueue(v);
        }
        int safety = 0;
        while(toCheck.TryDequeue(out var cur) && safety++ < 1000)
        {
            if (!ChunkManager.TryGetBlock(cur, out var curBlock)) continue;
            if (curBlock.WallBlock is LightBlock)
            {
                updated[cur.ToVector3Int()] = curBlock.LightLevel;
                continue;
            }
            ChunkManager.TryGetBlock(cur + Vector2Int.up, out var up);
            ChunkManager.TryGetBlock(cur + Vector2Int.down, out var down);
            ChunkManager.TryGetBlock(cur + Vector2Int.left, out var left);
            ChunkManager.TryGetBlock(cur + Vector2Int.right, out var right);

            var lrMax = Mathf.Max(left.LightLevel, right.LightLevel);
            bool lrSame = left.LightLevel == right.LightLevel;
            var udMax = Mathf.Max(up.LightLevel, down.LightLevel);
            bool udSame = up.LightLevel == down.LightLevel;

            var target = Mathf.Max(lrMax - (lrSame ? 0 : 1), udMax - (udSame ? 0 : 1));
            if (curBlock.LightLevel != target)
            {
                curBlock.LightLevel = updated[cur.ToVector3Int()] = target;
                foreach (var v in Utilities.QuadAdjacent.Select(v => v+cur))
                {
                    toCheck.Enqueue(v);
                }
            }
        }
        CallbackManager.AddCallback(() =>
        {
            OnLightingUpdated?.Invoke(updated);
        });
    }

    Vector2Int WorldToLocal(Vector2Int worldPos)
    {
        return worldPos - BlockPos;
    }

    TileDisplayCache tileDisplayCache;
    public TileDisplayCache GetBlocks()
    {
        return tileDisplayCache;
    }

    public void RegenCache()
    {
        Task.Run(() =>
        {
            tileDisplayCache = new TileDisplayCache(blocks, BlockPos);
        });
    }

    void ChangedBlock(Vector2Int worldPosition, BlockSlice slice)
    {
        curGenerator.SaveChunk(this);
        OnBlockChanged?.Invoke(this, worldPosition, ChunkPos, slice);
        RegenCache();
    }

    void RefreshBlock(Vector2Int worldPosition)
    {
        curGenerator.SaveChunk(this);
        OnBlockRefreshed?.Invoke(this, worldPosition, ChunkPos);
    }
}
