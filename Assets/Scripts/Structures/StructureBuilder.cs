using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureBuilder
{
    public readonly int chunkWidth;
    public Dictionary<Vector2Int, AnchorInfo> OpenAnchors = new Dictionary<Vector2Int, AnchorInfo>();
    public Dictionary<Vector2Int, BuildingBlockSlice[,]> chunks = new();

    Vector2Int selfPoint;
    IEnumerable<Vector2Int> SurroundingPoints;

    public StructureBuilder(int chunkWidth, Vector2Int selfPoint, IEnumerable<Vector2Int> SurroundingPoints)
    {
        this.chunkWidth = chunkWidth;
        this.selfPoint = selfPoint;
        this.SurroundingPoints = SurroundingPoints;
    }

    public bool TryGetBlock(Vector2Int worldPos, out BuildingBlockSlice block)
    {
        block = null;
        var (chunkPos, local) = WorldToLocalChunk(worldPos);
        if (Vector2Int.Distance(chunkPos * chunkWidth, selfPoint) > SurroundingPoints.Select(v => Vector2Int.Distance(chunkPos, v)).Min())
        {
            return false;
        }
        if (!chunks.TryGetValue(chunkPos, out var blocks))
        {
            blocks = new BuildingBlockSlice[chunkWidth, chunkWidth];
            chunks[chunkPos] = blocks;
        }
        block = blocks[local.x, local.y];
        return true;
    }

    public void SetBlock(Vector2Int worldPos, BuildingBlockSlice block)
    {
        var (chunkPos, local) = WorldToLocalChunk(worldPos);
        if(!chunks.TryGetValue(chunkPos, out var blocks))
        {
            blocks = new BuildingBlockSlice[chunkWidth, chunkWidth];
            chunks[chunkPos] = blocks;
        }
        blocks[local.x, local.y] = block;
    }

    (Vector2Int chunk, Vector2Int local) WorldToLocalChunk(Vector2Int worldPos)
    {
        var chunkPos = Vector2Int.FloorToInt(new Vector2(worldPos.x, worldPos.y) / chunkWidth);
        return (chunkPos, worldPos - (chunkPos * chunkWidth));
    }

    internal Dictionary<Vector2Int, BuildingBlockSlice[,]> GetDicts()
    {
        return chunks;
    }
}
