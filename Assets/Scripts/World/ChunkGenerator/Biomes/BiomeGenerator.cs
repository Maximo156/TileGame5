using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BiomeGenerator : ChunkSubGenerator
{
    public List<BiomePreset> Biomes;
    public override Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache)
    {
        throw new NotImplementedException();
    }
}
