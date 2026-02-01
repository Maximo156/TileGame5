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
        NativeList<int2> chunks,
        RealmInfo realmInfo)
    {

    }

    public abstract JobHandle ScheduleGeneration(
        int chunkWidth,
        NativeArray<int2> originalChunks,
        NativeArray<int2> requestChunks,
        RealmData realmData,
        RealmInfo realmInfo,
        ref BiomeData biomeData,
        JobHandle dep = default);

}

public struct RealmInfo
{
    public RealmBiomeInfo BiomeInfo;
    public RealmStructureInfo StructureInfo;
}
