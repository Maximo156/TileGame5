using NativeRealm;
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
    ConcurrentDictionary<Vector2Int, BuildingBlockSlice[,]> Loaded = new();
    public override Task UpdateBlockSlices(BlockSliceState[,] blocks, ChunkData data, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache)
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
            Overlay(blocks, data, structBlocks);
        }
        return Task.CompletedTask;
    }

    void Overlay(BlockSliceState[,] blocks, ChunkData data, BuildingBlockSlice[,] structure)
    {
        for (int x = 0; x < data.chunkWidth; x++)
        {
            for (int y = 0; y < data.chunkWidth; y++)
            {
                var StructureBlock = structure[x, y];
                if (StructureBlock?.HasBlock() == true)
                {
                    data.SetBlock(x, y, new()
                    {
                        groundBlock = StructureBlock.GroundBlock?.Id ?? 0,
                        roofBlock = StructureBlock.RoofBlock?.Id ?? 0,
                        wallBlock = StructureBlock.WallBlock?.Id ?? 0,
                    });
                    blocks[x,y].blockState = StructureBlock.State;
                }
            }
        }
    }
}
