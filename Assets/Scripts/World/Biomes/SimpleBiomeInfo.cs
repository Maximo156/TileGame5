using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine; 

[CreateAssetMenu(fileName = "NewRealmBiomeInfo", menuName = "Terrain/Biome/SimpleBiomeInfo", order = 1)]
[BurstCompile]
public class SimpleBiomeInfo : RealmBiomeInfo
{
    protected override JobHandle ScheduelInternalBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData)
    {
        var length = chunkWidth * chunkWidth * chunks.Length;
        var moistureMap = new NativeArray<float>(length, Allocator.Persistent);
        var heatMap = new NativeArray<float>(length, Allocator.Persistent);

        var moistureJob = MoistureSound?.ScheduleSoundJob(chunks, moistureMap, chunkWidth) ?? default;
        var heatJob = HeatSound?.ScheduleSoundJob(chunks, heatMap, chunkWidth) ?? default;

        var resolveBiomeJob = new ResolveBasicBiomesJob()
        {
            chunkLength = chunkWidth * chunkWidth,
            moistureArray = moistureMap,
            heatArray = heatMap,
            biomeInfo = BiomeInfo,
            biomeArray = biomeData.SelectedBiome
        }.Schedule(chunks.Length, 1, JobHandle.CombineDependencies(heatJob, moistureJob));

        return JobHandle.CombineDependencies(heatMap.Dispose(resolveBiomeJob), moistureMap.Dispose(resolveBiomeJob));
    }

    [BurstCompile]
    partial struct ResolveBasicBiomesJob : IJobParallelFor
    {
        public int chunkLength;

        [ReadOnly]
        public NativeArray<float> moistureArray;
        [ReadOnly]
        public NativeArray<float> heatArray;
        [ReadOnly]
        public NativeBiomeInfo biomeInfo;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> biomeArray;

        public void Execute(int index)
        {
            var chunkMoistureSlice = moistureArray.GetChunk(index, chunkLength);
            var chunkHeatSlice = heatArray.GetChunk(index, chunkLength);

            var chunkBiomeSlice = biomeArray.GetChunk(index, chunkLength);

            for (int i = 0; i < chunkLength; i++)
            {
                chunkBiomeSlice[i] = GetClosestBiomeIndex(chunkMoistureSlice[i], chunkHeatSlice[i], biomeInfo);
            }
        }

        int GetClosestBiomeIndex(float moisture, float heat, NativeBiomeInfo info)
        {
            if (info.Biomes.Length == 0)
            {
                return -1;
            }
            int index = 0;
            float dist = info.Biomes[0].DistSq(moisture, heat);

            for (int i = 1; i < info.Biomes.Length; i++)
            {
                var newDist = info.Biomes[i].DistSq(moisture, heat);
                if (newDist < dist)
                {
                    index = i;
                    dist = newDist;
                }
            }
            return index;
        }
    }
}
