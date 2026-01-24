using NativeRealm;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public abstract class ChunkSubGenerator: ScriptableObject
{
    public int Priority;
    public abstract Task UpdateBlockSlices(
        BlockSliceState[,] blocks, 
        ChunkData chunkData, 
        Vector2Int ChunkPosition, 
        Vector2Int WorldPosition, 
        BiomeInfo biomeInfo, 
        System.Random rand, 
        GenerationCache cache);
    public virtual void UpdateRequestedChunks(
        NativeList<int2> chunks)
    {

    }

    public abstract JobHandle ScheduleGeneration(
        int chunkWidth,
        NativeArray<int2> chunks,
        RealmData realmData,
        BiomeInfo biomeInfo,
        ref BiomeData biomeData,
        JobHandle dep = default);

}
