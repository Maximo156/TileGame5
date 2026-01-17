using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using NativeRealm;

[CreateAssetMenu(fileName = "NewTerrainGenerator", menuName = "Terrain/Generator", order = 1)]
public class TerrainGenerator : ChunkSubGenerator
{

    public override Task UpdateBlockSlices(BlockSliceState[,] blocks, ChunkData data, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache)
    {
        biomeInfo.UpdateBlockSlices(WorldPosition, blocks, data, rand, cache);
        return Task.CompletedTask;
    }
}
