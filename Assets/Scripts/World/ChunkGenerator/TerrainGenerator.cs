using UnityEngine;
using NativeRealm;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using BlockDataRepos;
using Unity.Burst;

[CreateAssetMenu(fileName = "NewTerrainGenerator", menuName = "Terrain/Generator", order = 1)]
public class TerrainGenerator : ChunkSubGenerator
{
    public BaseSoundSettings SparceBlockSound;

    public override JobHandle ScheduleGeneration(int chunkWidth, NativeArray<int2> originalChunks, NativeArray<int2> chunks, RealmData realmData, RealmInfo realmInfo, ref BiomeData biomeData, JobHandle dep = default)
    {
        var length = chunkWidth * chunkWidth * chunks.Length;

        var sparce = new NativeArray<float>(length, Allocator.Persistent);
        var sparceJob = SparceBlockSound.ScheduleSoundJob(chunks, sparce, chunkWidth);

        var biomeDataJob = realmInfo.BiomeInfo.ScheduelBiomeInfoGen(chunkWidth, chunks, ref biomeData);

        var mainJob = new GenerateBiomeBlocks()
        {
            chunks = chunks,
            chunkWidth = chunkWidth,
            SparceBlockDensity = sparce,
            biomeInfo = realmInfo.BiomeInfo.BiomeInfo,
            realmData = realmData.AsParallelWriter(),
            biomeData = biomeData,
        }.Schedule(chunks.Length, 1, JobHandle.CombineDependencies(
                dep,
                biomeDataJob,
                sparceJob
            ));

        var cleanup = sparce.Dispose(mainJob);

        return cleanup;
    }

    [BurstCompile]
    partial struct GenerateBiomeBlocks : IJobParallelFor
    {
        public int chunkWidth;

        [ReadOnly]
        public BiomeData biomeData;
        [ReadOnly]
        public NativeArray<float> SparceBlockDensity;
        [ReadOnly]
        public NativeArray<int2> chunks;
        [ReadOnly]
        public NativeBiomeInfo biomeInfo;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public RealmData.ParallelWriter realmData;

        public void Execute(int index)
        {
            var chunkLength = chunkWidth * chunkWidth;
            var chunk = chunks[index];
            var data = realmData.InitChunk(chunk, index);
            var sparceBlockDensity = SparceBlockDensity.GetChunk(index, chunkLength);
            var heatMap = biomeData.HeatMap.GetChunk(index, chunkLength);
            var heightMap = biomeData.HeightMap.GetChunk(index, chunkLength);
            var moistureMap = biomeData.MoistureMap.GetChunk(index, chunkLength);

            var random = new Random((uint)chunk.GetHashCode());
            for (int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    var foundWall = biomeInfo.TryGetWall(heightMap.GetElement2d(x, y, chunkWidth), out var wallBiome);
                    var foundBiome = biomeInfo.TryGetBiome(
                        heightMap.GetElement2d(x, y, chunkWidth),
                        moistureMap.GetElement2d(x, y, chunkWidth),
                        heatMap.GetElement2d(x, y, chunkWidth), out var biome);

                    var slice = new NativeBlockSlice() { isWater = true };
                    if (!foundBiome)
                    {
                        data.InitializeSlice(x, y, slice);
                        continue;
                    }

                    slice.isWater = false;

                    if (!foundWall)
                    {
                        slice.groundBlock = biome.groundBlock;
                        SetSparce(x, y, ref slice, biomeInfo.GetSparceBlocks(biome), sparceBlockDensity, biome.sparceDesnity, biome.sparceReplaceSolid, ref random);
                    }
                    else
                    {
                        slice.groundBlock = biomeInfo.GetReplacementInfo(biome, wallBiome.groundBlock);
                        slice.wallBlock = biomeInfo.GetReplacementInfo(biome, wallBiome.wallBlock);
                        slice.roofBlock = biomeInfo.GetReplacementInfo(biome, wallBiome.roofBlock);
                        SetSparce(x, y, ref slice, biomeInfo.GetSparceBlocks(wallBiome), sparceBlockDensity, wallBiome.sparceDesnity, wallBiome.sparceReplaceSolid, ref random);
                    }
                    data.InitializeSlice(x, y, slice);
                }
            }
        }

        public void SetSparce(int x, int y, ref NativeBlockSlice slice, NativeSlice<NativeSparceInfo> spaceBlocks, NativeSlice<float> sparceBlockDensity, float density, bool replaceSolid, ref Random random)
        {
            if (spaceBlocks.Length > 0 &&
                     Mathf.Pow(density * sparceBlockDensity.GetElement2d(x, y, chunkWidth), 1.7f) > random.NextDouble())
            {
                var sparce = spaceBlocks.SelectRandomWeighted(ref random);
                if (sparce.blockLevel == BlockLevel.Floor && (replaceSolid || slice.groundBlock == 0))
                    slice.groundBlock = sparce.block;
                if (sparce.blockLevel == BlockLevel.Wall && (replaceSolid || slice.wallBlock == 0))
                    slice.wallBlock = sparce.block;
            }
        }
    }
}
