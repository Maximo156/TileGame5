using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using NativeRealm;
using BlockDataRepos;
using static UnityEditor.Progress;
using UnityEngine.SocialPlatforms;
using Unity.Profiling;
using UnityEngine.Rendering;

public partial class Chunk
{
    public delegate void BlockChanged(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos, NativeBlockSlice block, BlockSliceState state);
    public event BlockChanged OnBlockChanged;

    public delegate void BlockRefreshed(Chunk chunk, Vector2Int BlockPos, Vector2Int ChunkPos);
    public event BlockRefreshed OnBlockRefreshed;

    public delegate void LightingUpdated(Dictionary<Vector3Int, int> updated);
    //public event LightingUpdated OnLightingUpdated;

    public delegate void ChunkChanged(Chunk chunk);
    public event ChunkChanged OnChunkChanged;

    public Vector2Int ChunkPos;
    public Vector2Int BlockPos => ChunkPos * width; 

    public Dictionary<Vector2Int, BlockSliceState> BlockStates {  get; set; }

    readonly int width;
    System.Random rand;

    Realm parentRealm;

    ChunkData data;
    public Chunk(Vector2Int chunkPos, int width, ChunkData data, Realm parentRealm)
    {
        this.width = width;
        ChunkPos = chunkPos;
        this.data = data;
        this.parentRealm = parentRealm;
        rand = new System.Random(ChunkPos.GetHashCode());
        BlockStates = new();
        Task.Run(() => InitCache());
    }

    void InitCache()
    {
        tileDisplayCache = new TileDisplayCache(BlockStates, data, BlockPos);
        CallbackManager.AddCallback(() => OnChunkChanged?.Invoke(this));
    }

    public void ChunkTick(CancellationToken cancellationToken)
    {
        bool updated = false;
        List<(Vector2Int pos, NativeBlockSlice slice, BlockSliceState state)> updateInfo = new List<(Vector2Int pos, NativeBlockSlice slice, BlockSliceState state)>();
        for(int x = 0; x < data.chunkWidth; x++)
        {
            for (int y = 0; y < data.chunkWidth; y++)
            {
                continue;
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var local = new Vector2Int(x, y);
                var sliceState = BlockStates.GetValueOrDefault(local);
                var state = sliceState?.blockState;
                var sliceData = data.GetSlice(x, y);
                var worldPos = local + BlockPos;
                var sliceUpdated = false;
                if (BlockDataRepo.GetBlock<Wall>(sliceData.wallBlock) is ITickableBlock wallBlock)
                {
                    var tickRes = wallBlock.Tick(worldPos, state, rand);
                    if(tickRes != 0)
                    {
                        SetBlock(x, y, tickRes);
                        sliceUpdated |= true;
                    }
                }
                if (BlockDataRepo.GetBlock<Wall>(sliceData.groundBlock) is ITickableBlock floorBlock)
                {
                    var tickRes = floorBlock.Tick(worldPos, state, rand);
                    if (tickRes != 0)
                    {
                        SetBlock(x, y, tickRes);
                        sliceUpdated |= true;
                    }
                }

                if (sliceUpdated)
                {
                    updated |= true;
                    updateInfo.Add((worldPos, data.GetSlice(x, y), sliceState));
                }
            }
        }
        if (updated)
        {
            RegenCache();
            CallbackManager.AddCallback(() =>
            {
                foreach (var (pos, slice, state) in updateInfo)
                {
                    OnBlockChanged?.Invoke(this, pos, ChunkPos, slice, state);
                }
            });
        }
    }

    public NativeBlockSlice GetBlock(Vector2Int world)
    {
        var localPos = WorldToLocal(world);
        return data.GetSlice(localPos.x, localPos.y);
    }

    public BlockSliceState GetBlockState(Vector2Int world)
    {
        var localPos = WorldToLocal(world);
        return BlockStates.GetValueOrDefault(localPos);
    }

    public bool SetBlock(int x, int y, ushort blockId, byte initialState = 0)
    {
        var nativeData = BlockDataRepo.GetNativeBlock(blockId);
        if (nativeData.Level == BlockLevel.Wall)
        {
            var blockInfo = BlockDataRepo.GetBlock<Block>(blockId);
            if (nativeData.lightLevel != 0)
            {
                Debug.LogWarning("Need to fix lighting");
            }
            data.SetWall(x, y, blockId);
            data.SetState(x, y, initialState != 0 ? initialState : blockInfo.GetDefaultSimpleState());
            var local = new Vector2Int(x, y);
            if (!BlockStates.TryGetValue(local, out var state))
            {
                state = new();
                BlockStates[local] = state;
            }
            state.blockState = blockInfo.GetState();
        }
        if (nativeData.Level == BlockLevel.Roof)
        {
            data.SetRoof(x, y, blockId);
        }
        else if (nativeData.Level == BlockLevel.Floor)
        {
            data.SetFloor(x, y, blockId);
        }
        return true;
        
    }

    public bool SafeSet(int x, int y, ushort blockId, byte initialState = 0)
    {
        if (CanPlace(x, y, blockId))
        {
            SetBlock(x, y, blockId, initialState);
            return true;
        }
        return false;
    }

    public bool CanPlace(Vector2Int worldPos, ushort blockId)
    {
        var local = WorldToLocal(worldPos);
        return CanPlace(local.x, local.y, blockId);
    }

    bool CanPlace(int x, int y, ushort blockId)
    {
        var local = new Vector2Int(x, y);
        var curSlice = data.GetSlice(x, y);
        var curState = BlockStates.GetValueOrDefault(local);
        var nativeData = BlockDataRepo.GetNativeBlock(blockId);
        var mustBePlacedOn = BlockDataRepo.GetMustBePlacedOn(nativeData);

        if (nativeData.Level == BlockLevel.Wall &&
            (curSlice.wallBlock != 0 ||
            (curState != null && curState.placedItems != null && curState.placedItems.Count > 0) ||
            (mustBePlacedOn.Length > 0 && !mustBePlacedOn.Contains(curSlice.groundBlock)))
            )
        {
            return false;
        }
        if (nativeData.Level == BlockLevel.Roof && curSlice.roofBlock != 0)
        {
            return false;
        }
        else if (nativeData.Level == BlockLevel.Floor && (curSlice.groundBlock != 0 || curSlice.wallBlock != 0))
        {
            return false;
        }
        return true;
    }

    bool Break(Vector2Int worldPos, bool roof, out Block broken, bool dontDrop = false)
    {
        ushort brokenId;
        var local = WorldToLocal(worldPos);
        var x = local.x;
        var y = local.y;
        var slice = data.GetSlice(x, y);
        var state = BlockStates.GetValueOrDefault(local);
        if (roof)
        {
            brokenId = slice.roofBlock;
            slice.roofBlock = 0;
            data.SetRoof(x, y, 0);
        }
        else if (slice.wallBlock != 0)
        {
            brokenId = slice.wallBlock;
            slice.wallBlock = 0;
            data.SetWall(x, y, 0);
            data.SetState(x, y, 0);
            state?.DropItems(worldPos);
        }
        else
        {
            brokenId = slice.groundBlock;
            slice.groundBlock = 0;
            data.SetFloor(x, y, 0);
            state?.DropItems(worldPos);
        }
        if (BlockDataRepo.TryGetBlock(brokenId, out broken))
        {
            broken.OnBreak(worldPos, new Block.BreakInfo() { state = state?.blockState, slice = slice, dontDrop = dontDrop });
        }
        return broken is Roof || (broken is Wall && slice.roofBlock != 0);
    }

    public bool PlaceBlock(Vector2Int position, Vector2Int dir, Block block, bool force = false, byte initialState = 0)
    {
        if(block is IConditionalPlace cond && !cond.CanPlace(position, dir))
        {
            return false;
        }
        var localPos = WorldToLocal(position);
        var x = localPos.x;
        var y = localPos.y;
        var res = force ? SetBlock(x, y, block.Id, initialState) : SafeSet(x, y, block.Id, initialState);
        if (res)
        {
            var slice = GetBlock(position);
            if (block is IOnPlace place)
            {
                place.OnPlace(position, dir, ref slice);
                data.InitializeSlice(x, y, slice);
            }
            if (block is Roof roof) {
                CalcRoofStrengthBFS(position, roof);
            }
            ChangedBlock(position, slice, BlockStates.GetValueOrDefault(localPos), block);
        }
        return res;
    }

    public bool BreakBlock(Vector2Int worldPosition, bool roof, bool drop = true)
    {
        if(Break(worldPosition, roof, out var broken, !drop))
        {
            CalcRoofStrengthBFS(worldPosition, broken as Roof);
        }
        var slice = GetBlock(worldPosition);
        var state = GetBlockState(worldPosition);
        ChangedBlock(worldPosition, slice, state, broken);
        return true;
    }

    public bool PlaceItem(Vector2Int worldPosition, ItemStack item)
    {
        var slice = GetBlock(worldPosition);

        var state = GetBlockState(worldPosition);
        if (state == null) return false;

        var res = state.PlaceItem(item, slice);

        ChangedSlice(worldPosition, slice, state);
        return res;
    }

    public ItemStack PopItem(Vector2Int worldPosition)
    {
        var slice = GetBlock(worldPosition);

        var state = GetBlockState(worldPosition);
        if (state == null) return null;

        var item = state.PopItem();
        ChangedSlice(worldPosition, slice, state);
        return item;
    }

    public bool Interact(Vector2Int worldPosition)
    {
        var local = WorldToLocal(worldPosition);
        var nativeSlice = GetBlock(worldPosition);
        var origSlice = nativeSlice;
        if (BlockDataRepo.TryGetBlock<Wall>(nativeSlice.wallBlock, out var wall) && wall is IInteractableBlock interactable)
        {
            if (interactable.Interact(worldPosition, ref nativeSlice))
            {
                data.InitializeSlice(local.x, local.y, nativeSlice);
                if (origSlice.wallBlock == nativeSlice.wallBlock)
                {
                    RefreshBlock(worldPosition);
                }
                else
                {
                    ChangedSlice(worldPosition, nativeSlice, BlockStates.GetValueOrDefault(local));
                }
            }
            return true;
        }
        return false;
    }

    public void CalcRoofStrengthBFS(Vector2Int worldPosition, Roof curRoof)
    {
        ChunkManager.TryGetBlock(worldPosition, out var initialSlice);
        var roofStrength = curRoof?.Strength ?? BlockDataRepo.GetBlock<Roof>(initialSlice.roofBlock).Strength;
        var radius = roofStrength * 2;
        var grid = new int[2 * radius + 1, 2 * radius + 1];
        Queue<Vector2Int> toCheck = new();
        for(int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var local = new Vector2Int(x, y);

                if (ChunkManager.TryGetBlock(localToWorld(local), out var s) && BlockDataRepo.GetBlock<Wall>(s.wallBlock)?.structural == true && BlockDataRepo.GetBlock<Roof>(s.roofBlock) is not null)
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
                if(0 <= newPos.x && newPos.x < radius * 2 - 1 && 0 <= newPos.y && newPos.y < radius * 2 - 1 && ChunkManager.TryGetBlock(localToWorld(newPos), out var s) && BlockDataRepo.GetBlock<Roof>(s.roofBlock) is not null)
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
                var local = new Vector2Int(x, y);
                var worldpos = localToWorld(local);
                if (ChunkManager.TryGetBlock(worldpos, out var s)) {
                    var roofBlock = BlockDataRepo.GetBlock<Wall>(s.roofBlock);
                    if (BlockDataRepo.TryGetNativeBlock(s.roofBlock, out var roof) && roof.Level == BlockLevel.Roof && roof.roofStrength < grid[x, y] && worldpos.ManhattanDistance(worldPosition) <= roofStrength)
                    {
                        Break(worldpos, true, out var _);
                        OnBlockChanged?.Invoke(this, worldpos, ChunkPos, data.GetSlice(x, y), BlockStates.GetValueOrDefault(local));
                    }
                }
            }
        }

        Vector2Int localToWorld(Vector2Int local)
        {
            return worldPosition + local - Vector2Int.one * (radius - 1);
        }
    }

    Vector2Int WorldToLocal(Vector2Int worldPos)
    {
        return worldPos - BlockPos;
    }

    public TileDisplayCache tileDisplayCache {  get; private set; } 

    public void RegenCache()
    {
        Task.Run(() =>
        {
            tileDisplayCache = new TileDisplayCache(BlockStates, data, BlockPos);
        });
    }

    void ChangedBlock(Vector2Int worldPosition, NativeBlockSlice slice, BlockSliceState state, Block updated)
    {
        ChangedSlice(worldPosition, slice, state);
    }

    void ChangedSlice(Vector2Int worldPosition, NativeBlockSlice slice, BlockSliceState state)
    {
        var wToL = WorldToLocal(worldPosition);
        //curGenerator.SaveChunk(this);
        Debug.LogWarning("Fix chunk saving");
        OnBlockChanged?.Invoke(this, worldPosition, ChunkPos, slice, state);
        RegenCache();
    }

    void RefreshBlock(Vector2Int worldPosition)
    {
        //curGenerator.SaveChunk(this);
        Debug.LogWarning("Fix chunk saving");
        OnBlockRefreshed?.Invoke(this, worldPosition, ChunkPos);
    }
}
