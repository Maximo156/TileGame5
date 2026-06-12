using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRealmBiomeInfo", menuName = "Terrain/Biome/SimpleBiomeInfo", order = 1)]
public class SimpleBiomeInfo : RealmBiomeInfo
{
    public override JobHandle ScheduelBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData)
    {
        var heightJob = HeightSound.ScheduleSoundJob(chunks, biomeData.HeightMap, chunkWidth);

        var biomeJob = BiomeResolver.ResolveBasicBiomes(chunkWidth, chunks, MoistureSound, HeatSound, ref biomeData, BiomeInfo);

        return JobHandle.CombineDependencies(biomeJob, heightJob);
    }
}
