using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using NativeRealm;
using BlockDataRepos;

[CreateAssetMenu(fileName = "NewBiomePreset", menuName = "Terrain/Biome/BiomePreset", order = 1)]
public class BiomePreset : ScriptableObject
{
    [Serializable]
    public struct ReplacementInfo
    {
        public Block Original;
        public Block Replacement;
    }

    [Serializable]
    public class MobSpawnInfo
    {
        public int MinCount;
        public int MaxCount;
        public int Weight;

        public GameObject Prefab;
    }

    [Header("Gen Info")]
    public float minMoisture;
    public float minHeat;
    public float minHeight;

    [Header("Block Info")]
    public Ground GroundBlock;
    public Wall WallBlock;
    public Roof RoofBlock;
    public List<ReplacementInfo> ReplacementInfos = new();

    [Serializable]
    public class SparceBlockInfo
    {
        public string Name => block.name;
        public int Weight;
        public Block block;
    }
    [Header("Sparce Blocks")]
    public bool replaceSolid = false;
    public List<SparceBlockInfo> SparceBlockInfos;
    public float SparceDensity;

    [Header("SpawnInfo")]
    public List<MobSpawnInfo> NaturalMobs;
    public List<MobSpawnInfo> HostileMobs;

    public bool MatchCondition(float moisture, float heat)
    {
        return moisture >= minMoisture && heat >= minHeat;
    }

    public float GetDiffValue(float moisture, float heat)
    {
        return (moisture - minMoisture) + (heat - minHeat);
    }

    public NativeBiomePreset GetNativePreset()
    {
        return new()
        {
            groundBlock = GroundBlock?.Id ?? 0,
            wallBlock = WallBlock?.Id ?? 0,
            roofBlock = RoofBlock?.Id ?? 0,
            minHeat = minHeat,
            minHeight = minHeight,
            minMoisture = minMoisture,
            sparceDesnity = SparceDensity,
            sparceReplaceSolid = replaceSolid
        };
    }
}

public struct NativeSparceInfo : IWeighted
{
    public BlockLevel blockLevel;
    public ushort block;
    public int Weight { get; set; }
}
