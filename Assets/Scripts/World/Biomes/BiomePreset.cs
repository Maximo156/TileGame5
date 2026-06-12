using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using NativeRealm;
using BlockDataRepos;
using ComposableBlocks;
using Unity.Mathematics;

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
    public float targetMoisture;
    public float targetHeat;
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

    [Header("Editor Only")]
    public Color32 EditorColor;

    public float DistSq(float moisture, float heat)
    {
        return math.distancesq(math.float2(heat, moisture), math.float2(targetHeat, targetMoisture));
    }

    public NativeBiomePreset GetNativePreset()
    {
        return new()
        {
            groundBlock = GroundBlock?.Id ?? 0,
            wallBlock = WallBlock?.Id ?? 0,
            roofBlock = RoofBlock?.Id ?? 0,
            targetHeat = targetHeat,
            minHeight = minHeight,
            targetMoisture = targetMoisture,
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
