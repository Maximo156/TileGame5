using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRealmBiomeInfo", menuName = "Terrain/Biome/RealmBiomeInfo", order = 1)]
public class RealmBiomeInfo : ScriptableObject
{
    public BaseSoundSettings HeightSound;
    public BaseSoundSettings MoistureSound;
    public BaseSoundSettings HeatSound;

    public List<BiomePreset> Biomes;
    public List<BiomePreset> WallBiomes;

    public float waterLevel = 0.2f;

    public BiomePreset GetBiome(Vector2Int worldPos)
    {
        return GetBiome(HeightSound.GetSound(worldPos.x, worldPos.y), MoistureSound?.GetSound(worldPos.x, worldPos.y) ?? 0, HeatSound?.GetSound(worldPos.x, worldPos.y) ?? 0);
    }

    BiomePreset GetBiome(float height, float moisture, float heat)
    {
        if (height <= waterLevel) return null;
        return Biomes.Where(b => b.MatchCondition(moisture, heat)).MinBy(b => b.GetDiffValue(moisture, heat));
    }

    NativeBiomeInfo biomeInfo;

    public NativeBiomeInfo BiomeInfo 
    { 
        get
        {
            if (!biomeInfo.isCreated)
            {
                biomeInfo = new NativeBiomeInfo(Biomes, WallBiomes, waterLevel);
            }
            return biomeInfo; 
        } 
    }

    public void OnEnable()
    {
        WallBiomes = WallBiomes.OrderBy(w => w.minHeight).ToList();
    }

    public void Dispose()
    {
        biomeInfo.Dispose(); 
    }

    public JobHandle ScheduelBiomeInfoGen(int chunkWidth, NativeArray<int2> chunks, ref BiomeData biomeData)
    {
        var heightJob = HeightSound.ScheduleSoundJob(chunks, biomeData.HeightMap, chunkWidth);
        var moistureJob = MoistureSound.ScheduleSoundJob(chunks, biomeData.MoistureMap, chunkWidth);
        var heatJob = HeatSound.ScheduleSoundJob(chunks, biomeData.HeatMap, chunkWidth);

        return JobHandle.CombineDependencies(heatJob, moistureJob, heightJob);
    }
}
 