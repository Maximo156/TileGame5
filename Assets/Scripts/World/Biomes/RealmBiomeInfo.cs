using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public abstract class RealmBiomeInfo : ScriptableObject
{
    public FractalSound HeightSound;
    public FractalSound MoistureSound;
    public FractalSound HeatSound;

    public List<BiomePreset> Biomes;
    public List<BiomePreset> WallBiomes;

    public float waterLevel = 0.2f;

    public BiomePreset GetBiome(Vector2Int worldPos)
    {
        return GetBiome(HeightSound.GetSound(worldPos.x, worldPos.y), MoistureSound?.GetSound(worldPos.x, worldPos.y) ?? 0, HeatSound?.GetSound(worldPos.x, worldPos.y) ?? 0);
    }

    public BiomePreset GetBiome(float height, float moisture, float heat)
    {
        if (height <= waterLevel) return null;
        return Biomes.MinBy(b => b.DistSq(moisture, heat));
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

    public abstract JobHandle ScheduelBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData);
}
 