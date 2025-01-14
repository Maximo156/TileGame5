using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[JsonObject(MemberSerialization.OptIn,  ItemNullValueHandling = NullValueHandling.Ignore)]
public class BlockSlice
{
    public bool Water = true;
    public float MovementSpeed => Water && GroundBlock is null ? 0.5f : (1 + (GroundBlock?.MovementModifier ?? 0) + (WallBlock?.MovementModifier ?? 0));
    public bool Walkable => WallBlock is null || WallBlock.Walkable;

    [JsonProperty]
    public Ground GroundBlock { get; private set; }
    [JsonProperty]
    public Wall WallBlock { get; private set; }
    [JsonProperty]
    public Roof RoofBlock { get; private set; }

    private int _lightLevel;
    public int LightLevel
    {
        get => ((WallBlock as LightBlock)?.LightLevel * 2) ?? _lightLevel;
        set => _lightLevel = value;
    }

    public Stack<ItemStack> PlacedItems;

    public BlockState State;

    public BlockSlice()
    {

    }

    public BlockSlice(BlockSlice reference)
    {
        if (reference != null)
        {
            SetBlock(reference.GroundBlock);
            SetBlock(reference.WallBlock);
            SetBlock(reference.RoofBlock);
        }
    }

    public bool SetBlock(Block block)
    {
        if(block is Wall wall)
        {
            if(WallBlock is LightBlock)
            {
                _lightLevel = 0;
            }
            WallBlock = wall;
            State = block.GetState();
        }
        if (block is Roof roof)
        {
            RoofBlock = roof;
        }
        else if (block is Ground ground)
        {
            GroundBlock = ground;
        }
        return true;
    }

    public bool SafeSet(Block block)
    {
        if (block is Wall wall &&
            (WallBlock is not null ||
            (PlacedItems != null && PlacedItems.Count > 0) ||
            (wall.MustBePlacedOn.Count > 0 && !wall.MustBePlacedOn.Contains(GroundBlock)))
            )
        {
            return false;
        }
        if (block is Roof && RoofBlock is not null)
        {
            return false;
        }
        else if (block is Ground && (GroundBlock is not null || WallBlock is not null || (PlacedItems != null && PlacedItems.Count > 0)))
        {
            return false;
        }
        SetBlock(block);
        return true;
    }

    public bool Break(Vector2Int worldPos, bool roof, out Block broken, bool dontDrop = false)
    {
        BlockState state = null;
        if (roof)
        {
            broken = RoofBlock;
            RoofBlock = null;
        }
        else if (WallBlock is not null)
        {
            broken = WallBlock;
            state = State;
            WallBlock = null;
            State = null;
            DropItems(worldPos);
        }
        else
        {
            broken = GroundBlock;
            GroundBlock = null;
            DropItems(worldPos);
        }
        broken?.OnBreak(worldPos, new Block.BreakInfo() { state = state, dontDrop = dontDrop });
        return broken is Roof || (broken is Wall && RoofBlock is not null);
    }

    public bool PlaceItem(ItemStack item)
    {
        if(item is null)
        {
            return false;
        }
        if(PlacedItems is null)
        {
            PlacedItems = new();
        }
        if (WallBlock is null && PlacedItems.Count == 0)
        {
            PlacedItems.Push(item);
            return true;
        }
        if (PlacedItems.TryPeek(out var placedItem) && placedItem.Item == item.Item)
        {
            PlacedItems.Peek().Combine(item);
            return item.Count == 0;
        }
        return false;
    }

    public ItemStack PopItem()
    {
        return PlacedItems?.Count == 0 ? null : PlacedItems?.Pop();
    }

    public bool Tick(Vector2Int worldPos, System.Random rand)
    {
        var updateWall = WallBlock is ITickableBlock wallTick ? wallTick.Tick(worldPos, this, rand) : false;
        var updateFloor = GroundBlock is ITickableBlock groundTick ? groundTick.Tick(worldPos, this, rand) : false;
        return updateWall || updateFloor;
    }

    void DropItems(Vector2Int worldPos)
    {
        if (PlacedItems is null) return;
        Utilities.DropItems(worldPos, PlacedItems);
        PlacedItems = null;
    }

    public bool HasBlock()
    {
        return GroundBlock is not null || WallBlock is not null || RoofBlock is not null;
    }
}
