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
    Dictionary<Block, Block> ReplacementInfosDict;

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
    public BaseSoundSettings SparceSoundSettings;
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

    public void SetSparce(Vector2Int worldPos, System.Random rand, ref NativeBlockSlice slice)
    {
        if (SparceSoundSettings is not null && SparceBlockInfos.Any() &&
                 Mathf.Pow(SparceDensity * SparceSoundSettings.GetSound(worldPos.x, worldPos.y), 1.7f) > rand.NextDouble())
        {
            var sparce = SparceBlockInfos.SelectRandomWeighted(b => b.Weight, b => b.block, rand);
                if (replaceSolid || (sparce is Ground && slice.groundBlock == 0))
                    slice.groundBlock = sparce.Id;
                if (replaceSolid || (sparce is Wall && slice.wallBlock == 0))
                    slice.wallBlock = sparce.Id;
        }
    }

    public Block GetReplacement(Block orig)
    {
        if (orig == null) return null;
        if(ReplacementInfosDict.TryGetValue(orig, out var replacement))
        {
            return replacement;
        }
        return orig;
    }

    private void OnEnable()
    {
        ReplacementInfosDict = ReplacementInfos.ToDictionary(info => info.Original, info => info.Replacement);
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
