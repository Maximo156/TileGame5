using BlockDataRepos;
using NativeRealm;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = Unity.Mathematics.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

[CreateAssetMenu(fileName = "NewBiomeInfo", menuName = "Terrain/Biome/BiomeInfo", order = 1)]
public class BiomeInfo : ScriptableObject
{
    public BaseSoundSettings HeightSound;
    public BaseSoundSettings MoistureSound;
    public BaseSoundSettings HeatSound;
    public BaseSoundSettings SparceBlockSound;

    public List<BiomePreset> Biomes;
    public List<BiomePreset> WallBiomes;

    public float waterLevel = 0.2f;

    public BiomePreset GetBiome(Vector2Int worldPos)
    {
        return GetBiome(HeightSound.GetSound(worldPos.x, worldPos.y), MoistureSound?.GetSound(worldPos.x, worldPos.y) ?? 0, HeatSound?.GetSound(worldPos.x, worldPos.y) ?? 0);
    }

    public void UpdateBlockSlices(Vector2Int worldPos, BlockSliceState[,] blocks, ChunkData data, System.Random rand, GenerationCache cache)
    {
        int chunkWidth = blocks.GetLength(0);
        var heightMap = cache.HeightMap = HeightSound.GetSoundArray(worldPos.x, worldPos.y, chunkWidth);
        var moistureMap = cache.MoistureMap = MoistureSound?.GetSoundArray(worldPos.x, worldPos.y, chunkWidth);
        var heatMap = cache.HeatMap = HeatSound?.GetSoundArray(worldPos.x, worldPos.y, chunkWidth);

        for(int x = 0; x< blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(1); y++)
            {
                blocks[x, y] = new();
                var wallBiome = GetWall(heightMap[x, y]);
                var biome = GetBiome(heightMap[x, y], moistureMap?[x, y] ?? 0, heatMap?[x, y] ?? 0);

                var slice = new NativeBlockSlice() { isWater = true };
                if (biome is null)
                {
                    data.InitializeSlice(x, y, slice);
                    continue;
                }

                slice.isWater = false;

                if (wallBiome is null)
                {
                    slice.groundBlock = biome.GroundBlock.Id;
                    biome.SetSparce(worldPos, rand, ref slice);
                }
                else
                {
                    slice.groundBlock = biome.GetReplacement(wallBiome.GroundBlock)?.Id ?? 0;
                    slice.wallBlock = biome.GetReplacement(wallBiome.WallBlock)?.Id ?? 0;
                    slice.roofBlock = biome.GetReplacement(wallBiome.RoofBlock)?.Id ?? 0;
                    wallBiome.SetSparce(worldPos, rand, ref slice);
                }
                data.InitializeSlice(x, y, slice);
            }
        }
    }

    public (float hight, float moisture, float heat) GetWorldInfo(Vector2Int pos)
    {
        return (HeightSound.GetSound(pos.x, pos.y), MoistureSound?.GetSound(pos.x, pos.y) ?? 0, HeatSound?.GetSound(pos.x, pos.y) ?? 0);
    }

    BiomePreset GetBiome(float height, float moisture, float heat)
    {
        if (height <= waterLevel) return null;
        return Biomes.Where(b => b.MatchCondition(moisture, heat)).MinBy(b => b.GetDiffValue(moisture, heat));
    }

    BiomePreset GetWall(float height)
    {
        return WallBiomes.LastOrDefault(w => height > w.minHeight);
    }

    public bool IsWallBiome(float height)
    {
        return GetWall(height) is not null;
    }

    public NativeBiomeInfo biomeInfo { get; private set; }

    public void OnEnable()
    {
        WallBiomes = WallBiomes.OrderBy(w => w.minHeight).ToList();
    }

    public void Dispose()
    {
        biomeInfo.Dispose(); 
    }

    public JobHandle ScheduleGeneration(int chunkWidth, NativeArray<int2> chunks, RealmData realmData, ref BiomeData biomeData, JobHandle dep)
    {
        if (!biomeInfo.isCreated)
        {
            biomeInfo = new NativeBiomeInfo(Biomes, WallBiomes, waterLevel);
        }

        var length = chunkWidth * chunkWidth * chunks.Length;

        var sparce = new NativeArray<float>(length, Allocator.Persistent);

        var heightJob = HeightSound.ScheduleSoundJob(chunks, biomeData.HeightMap, chunkWidth);
        var moistureJob = MoistureSound.ScheduleSoundJob(chunks, biomeData.MoistureMap, chunkWidth);
        var heatJob = HeatSound.ScheduleSoundJob(chunks, biomeData.HeatMap, chunkWidth);
        var sparceJob = SparceBlockSound.ScheduleSoundJob(chunks, sparce, chunkWidth);

        var mainJob = new GenerateBiomeBlocks()
        {
            chunks = chunks,
            chunkWidth = chunkWidth,
            SparceBlockDensity = sparce,
            biomeInfo = biomeInfo,
            realmData = realmData.AsParallelWriter(),
            biomeData = biomeData,
        }.Schedule(chunks.Length, 1, JobHandle.CombineDependencies(
            dep,
            JobHandle.CombineDependencies(heatJob, moistureJob),
            JobHandle.CombineDependencies(heightJob, sparceJob)
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
                if (replaceSolid || (sparce.blockLevel == BlockLevel.Floor && slice.groundBlock == 0))
                    slice.groundBlock = sparce.block;
                if (replaceSolid || (sparce.blockLevel == BlockLevel.Wall && slice.wallBlock == 0))
                    slice.wallBlock = sparce.block;
            }
        }
    }
}
 