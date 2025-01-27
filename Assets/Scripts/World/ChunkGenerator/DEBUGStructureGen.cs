using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDEBUGStructureGenerator", menuName = "Terrain/DEBUGStructureGenerator", order = 1)]
public class DEBUGStructureGen : ChunkSubGenerator
{
    public Structure structure;
    ConcurrentDictionary<Vector2Int, BlockSlice[,]> Loaded = new();
    public override Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache)
    {
        if (ChunkPosition == Vector2Int.zero)
        {
            foreach (var kvp in structure.Generate(Vector2Int.zero, biomeInfo, blocks.GetLength(0), rand, new Vector2Int[] { Vector2Int.one * 1000 }))
            {
                Loaded.TryAdd(kvp.Key, kvp.Value);
            }
        }
        if (Loaded.Remove(ChunkPosition, out var structBlocks))
        {
            Overlay(ref blocks, structBlocks);
        }
        return Task.CompletedTask;
    }

    void Overlay(ref BlockSlice[,] blocks, BlockSlice[,] structure)
    {
        for (int x = 0; x < blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(0); y++)
            {
                if (structure[x, y]?.HasBlock() == true)
                {
                    var StructureBlock = structure[x, y];
                    var initialBlock = blocks[x, y];
                    StructureBlock.Water = initialBlock.Water;
                    blocks[x, y] = StructureBlock;
                }
            }
        }
    }
}
