using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NewVoronoiBiomeInfo", menuName = "Terrain/Biome/VoronoiBiomeInfo", order = 1)]
[BurstCompile]
public class VoronoiBiomeInfo : RealmBiomeInfo
{
    public uint seed => (uint)(name.GetHashCode() ^ WorldSave.ActiveSeed);

    [Header("Voronoi Settings")]
    public int ChunkWidth;
    public int SmoothingPasses;
    [Range(0.0f, 1.0f)]
    public float DisplacementPercent;
    public int DisplacementFrequency;

    protected override int GetBiomeIndex(Vector2Int worldPos)
    {
        var c = Utilities.GetChunk(worldPos.ToInt(), ChunkWidth);
        var v = GetData(c, 1);

        for (int i = 0; i < v.Length; i++)
        {
            var val = base.GetBiomeIndex(v.GetWorldPositionAndValue(i).pos.ToVector());
            v.SetValue(i, val);
        }

        DoSmoothingPasses(SmoothingPasses, ref v, Allocator.TempJob);

        var res = v.GetValue(worldPos.ToInt());
        v.Dispose();
        return res;
    }

    protected override JobHandle ScheduelInternalBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData)
    {
        var min = chunks[0];
        var max = chunks[0];
        foreach(var chunk in chunks)
        {
            min = math.min(min, chunk);
            max = math.max(max, chunk);
        }
        var dataWidth = (math.cmax(max - min) + 1) * chunkWidth / ChunkWidth + 1 ;
        var origin = Utilities.GetChunk(min * chunkWidth, ChunkWidth);

        var voronoiData = GetData(origin, dataWidth);

        var voronoiGen = new GenVoronoiData()
        {
            biomeInfo = BiomeInfo,
            heatSoundSettings = HeatSound.GetSettings(),
            moistureSoundSettings = MoistureSound.GetSettings(),
            smoothingPasses = SmoothingPasses,
            Voronoi = voronoiData,
        }.Schedule();

        var biomeGen = new GenBiomeInfo()
        {
            voronoiChunkWidth = ChunkWidth,
            displacementOffset = Utilities.Seededint2(4000, seed),
            displacementFrequency = DisplacementFrequency,
            displacementPercent = DisplacementPercent,
            chunkWidth = chunkWidth,
            chunks = chunks,
            voronoiBiomeData = voronoiData,
            biomeArray = biomeData.SelectedBiome
        }.Schedule(chunks.Length, 1, voronoiGen);

        return voronoiData.Dispose(biomeGen);
    }

    [BurstCompile]
    partial struct GenVoronoiData : IJob
    { 
        [ReadOnly]
        public NativeBiomeInfo biomeInfo;
        public int smoothingPasses;
        public FractalSound.FractalSoundSettings moistureSoundSettings; 
        public FractalSound.FractalSoundSettings heatSoundSettings;

        public VoronoiData<int> Voronoi;

        public void Execute()
        {
            for(int i = 0; i < Voronoi.Length; i++)
            {
                var pos = math.float2(Voronoi.GetWorldPositionAndValue(i).pos);
                var heat = FractalSound.GetSound(ref pos, ref heatSoundSettings);
                var moisture = FractalSound.GetSound(ref pos, ref moistureSoundSettings);
                Voronoi.SetValue(i, GetClosestBiomeIndex(moisture, heat, biomeInfo));
            }
            DoSmoothingPasses(smoothingPasses, ref Voronoi);
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

    [BurstCompile]
    partial struct GenBiomeInfo : IJobParallelFor
    {
        public int voronoiChunkWidth;
        public int chunkWidth;
        public int2 displacementOffset;
        public float displacementPercent;
        public int displacementFrequency;
        [ReadOnly]
        public NativeArray<int2> chunks;
        [ReadOnly]
        public VoronoiData<int> voronoiBiomeData;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> biomeArray;

        public void Execute(int index)
        {
            var chunkBlockPos = chunks[index] * chunkWidth;
            var biomeSlice = biomeArray.GetChunk(index, chunkWidth * chunkWidth);
            for(int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    var p = CalcOffsetPos(chunkBlockPos + math.int2(x, y));
                    biomeSlice.SetElement2d(x, y, chunkWidth, voronoiBiomeData.GetValue(p));
                }
            }
        }

        int2 CalcOffsetPos(int2 pos)
        {
            var sx = pos + displacementOffset + new float2(0.001f, 0.007f);
            var sy = pos + displacementOffset + new float2(1023.001f, -1070.007f);
            return pos + math.int2(math.float2(noise.cnoise(sx / displacementFrequency), noise.cnoise(sy / displacementFrequency)) * voronoiChunkWidth * displacementPercent);
        }
    }

    VoronoiPositions GetPositions(int2 Origin, int dataWidth)
    {
        var actualOrigin = Origin - SmoothingPasses - 2;
        return new VoronoiPositions(actualOrigin, ChunkWidth, dataWidth + 2 * (SmoothingPasses + 2), seed);
    }

    VoronoiData<int> GetData(int2 Origin, int dataWidth)
    {
        return new VoronoiData<int>(GetPositions(Origin, dataWidth));
    }

    static void DoSmoothingPasses(int passes, ref VoronoiData<int> data, Allocator allocator = Allocator.Temp)
    {
        if (passes == 0) return;
        var dataRead = data;
        var dataTmp = new VoronoiData<int>(data.Positions, allocator);
        var dataWrite = dataTmp;
        for (int i = 0; i < passes; i++)
        {
            DoSmoothingPass(ref dataWrite, ref dataRead);
            (dataRead, dataWrite) = (dataWrite, dataRead);
        }
        dataRead.CopyTo(data);
        if (allocator != Allocator.Temp)
        {
            dataTmp.Dispose();
        }
    }

    static void DoSmoothingPass(ref VoronoiData<int> dataWrite, ref VoronoiData<int> dataRead, Allocator allocator = Allocator.Temp)
    {
        var set = new NativeHashMap<int, int>(8, allocator);
        for (int x = 1; x < dataWrite.Width - 1; x++)
        {
            for (int y = 1; y < dataWrite.Width - 1; y++)
            {
                var max = -1;
                var maxVal = -1;
                var thisVal = dataRead.GetWorldPositionAndValue(x, y).val;
                for (int x1 = -1; x1 <= 1; x1++)
                {
                    for (int y1 = -1; y1 <= 1; y1++)
                    {
                        if (x1 == 0 && y1 == 0) continue;
                        var val = dataRead.GetWorldPositionAndValue(x + x1, y + y1).val;
                        set.TryGetValue(val, out var sum);
                        var newSum = sum + 1;
                        set[val] = newSum;
                        if(newSum > max)
                        {
                            max = newSum;
                            maxVal = val;
                        }
                    }
                }
                if (set.ContainsKey(thisVal))
                {
                    dataWrite.SetValue(x, y, thisVal);
                }
                else
                {
                    Debug.Log($"overwriting {thisVal} -> {maxVal}");
                    dataWrite.SetValue(x, y, maxVal);
                }

                set.Clear();
            }
        }
        set.Dispose();
    }
}
