using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using NativeRealm;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

[CreateAssetMenu(fileName = "NewTerrainGenerator", menuName = "Terrain/Generator", order = 1)]
public class TerrainGenerator : ChunkSubGenerator
{
    public override Task UpdateBlockSlices(BlockSliceState[,] blocks, ChunkData data, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache)
    {
        biomeInfo.UpdateBlockSlices(WorldPosition, blocks, data, rand, cache);
        return Task.CompletedTask;
    }

    public override JobHandle ScheduleGeneration(int chunkWidth, NativeArray<int2> chunks, RealmData realmData, BiomeInfo biomeInfo, ref BiomeData biomeData, JobHandle dep = default)
    {
        return biomeInfo.ScheduleGeneration(chunkWidth, chunks, realmData, ref biomeData, dep);
    }
}
