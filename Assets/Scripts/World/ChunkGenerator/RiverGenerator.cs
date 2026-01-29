using NativeRealm;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRiverGenerator", menuName = "Terrain/RiverGenerator", order = 1)]
public class RiverGenerator : ChunkSubGenerator
{
    public BaseSoundSettings RiverSound;
    public BaseSoundSettings Reducer;
    public float RiverCuttoff;

    public override JobHandle ScheduleGeneration(int chunkWidth, NativeArray<int2> chunks, RealmData realmData, RealmBiomeInfo biomeInfo, ref BiomeData biomeData, JobHandle dep = default)
    {
        var length = chunkWidth * chunkWidth;
        var riverArray = new NativeArray<float>(length * chunks.Length, Allocator.Persistent);
        var reducerArray = new NativeArray<float>(length * chunks.Length, Allocator.Persistent);

        var riverJob = RiverSound.ScheduleSoundJob(chunks, riverArray, chunkWidth);
        var reducerJob = Reducer.ScheduleSoundJob(chunks, reducerArray, chunkWidth);

        var mainJob = new RiverJob()
        {
            realmData = realmData.AsParallelWriter(),
            chunkWidth = chunkWidth,
            RiverCuttoff = RiverCuttoff,
            BiomeData = biomeData,
            BiomeInfo = biomeInfo.BiomeInfo,
            RiverArrays = riverArray,
            ReducerArrays = reducerArray,
        }.Schedule(chunks.Length, 1, JobHandle.CombineDependencies(riverJob, reducerJob, dep));

        return JobHandle.CombineDependencies(riverArray.Dispose(mainJob), reducerArray.Dispose(mainJob));
    }

    partial struct RiverJob : IJobParallelFor
    {
        public int chunkWidth;
        public float RiverCuttoff;
        [ReadOnly]
        public BiomeData BiomeData;
        [ReadOnly]
        public NativeBiomeInfo BiomeInfo;
        [ReadOnly]
        public NativeArray<float> RiverArrays;
        [ReadOnly]
        public NativeArray<float> ReducerArrays;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public RealmData.ParallelWriter realmData;

        public void Execute(int index)
        {
            var chunkLength = chunkWidth * chunkWidth;
            var data = realmData.GetChunk(index);
            var riverArray = RiverArrays.GetChunk(index, chunkLength);
            var reducerArray = ReducerArrays.GetChunk(index, chunkLength);
            var heightMap = BiomeData.HeightMap.GetChunk(index, chunkLength);

            for (int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    if (BiomeInfo.TryGetWall(heightMap.GetElement2d(x, y, chunkWidth), out var _)) continue;
                    var riverSound = (1 - MathF.Abs(riverArray.GetElement2d(x, y, chunkWidth) - 0.5f)) - (reducerArray.GetElement2d(x, y, chunkWidth) * RiverCuttoff);
                    if (riverSound > 1 - RiverCuttoff)
                    {
                        data.InitializeSlice(x, y, new()
                        {
                            isWater = true,
                        });
                    }
                }
            }
        }
    }
}
