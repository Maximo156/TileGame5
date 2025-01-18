using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[CreateAssetMenu(fileName = "NewTerrainGenerator", menuName = "Terrain/Generator", order = 1)]
public class TerrainGenerator : ChunkSubGenerator
{

    public override Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand)
    {
        biomeInfo.UpdateBlockSlices(WorldPosition, blocks, rand);
        return Task.CompletedTask;
    }
}
