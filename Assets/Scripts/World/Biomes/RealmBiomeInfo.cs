using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

[BurstCompile]
public abstract class RealmBiomeInfo : ScriptableObject
{
    public FractalSound HeightSound;
    public FractalSound MoistureSound;
    public FractalSound HeatSound;

    public List<BiomePreset> Biomes;
    public List<BiomePreset> WallBiomes;

    public float waterLevel = 0.2f;

    public BiomePreset GetBiome(float height, float moisture, float heat)
    {
        var ind = GetBiomeIndex(height, moisture, heat);
        if (ind == -1) return null;
        return Biomes[ind];
    }

    public BiomePreset GetBiome(Vector2Int worldPos)
    {
        var ind = GetBiomeIndex(worldPos);
        if(ind == -1) return null;
        return Biomes[ind];
    }

    protected int GetBiomeIndex(float height, float moisture, float heat)
    {
        if (height <= waterLevel) return -1;
        return Biomes.Select((b, i) => (index: i, val: b)).MinBy(b => b.val.DistSq(moisture, heat)).index;
    }

    protected virtual int GetBiomeIndex(Vector2Int worldPos)
    {
        return GetBiomeIndex(HeightSound.GetSound(worldPos.x, worldPos.y), MoistureSound?.GetSound(worldPos.x, worldPos.y) ?? 0, HeatSound?.GetSound(worldPos.x, worldPos.y) ?? 0);
    }

    NativeBiomeInfo _biomeInfo;

    public NativeBiomeInfo BiomeInfo 
    { 
        get
        {
            if (!_biomeInfo.isCreated)
            {
                _biomeInfo = new NativeBiomeInfo(Biomes, WallBiomes, waterLevel);
            }
            return _biomeInfo; 
        } 
    }

    public bool TryGetBiome(float height, int index, out BiomePreset biome)
    {
        biome = default;

        if (height <= waterLevel || index == -1)
            return false;

        biome = Biomes[index];
        return true;
    }

    public void OnEnable()
    {
        WallBiomes = WallBiomes.OrderBy(w => w.minHeight).ToList();
    }

    public void Dispose()
    {
        _biomeInfo.Dispose(); 
    }

    public JobHandle ScheduelBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData)
    {
        var heightJob = HeightSound.ScheduleSoundJob(chunks, biomeData.HeightMap, chunkWidth);

        var resolveBiomeJob = ScheduelInternalBiomeInfoGen(chunkWidth, chunks, ref biomeData);

        return JobHandle.CombineDependencies(resolveBiomeJob, heightJob);
    }
    
    protected abstract JobHandle ScheduelInternalBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData);

    [BurstCompile]
    protected static int GetClosestBiomeIndex(float moisture, float heat, ref NativeBiomeInfo info)
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
 