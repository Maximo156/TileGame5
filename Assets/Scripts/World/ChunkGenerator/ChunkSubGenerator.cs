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

    public virtual void UpdateRequestedChunks(
        NativeList<int2> chunks)
    {

    }

    public abstract JobHandle ScheduleGeneration(
        int chunkWidth,
        NativeArray<int2> chunks,
        RealmData realmData,
        RealmBiomeInfo biomeInfo,
        ref BiomeData biomeData,
        JobHandle dep = default);

}
