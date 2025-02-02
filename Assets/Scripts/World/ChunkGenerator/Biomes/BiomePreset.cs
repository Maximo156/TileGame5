using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

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

    [Header("Sparce Blocks")]
    public bool replaceSolid = false;
    public List<Block> SparceBlocks;
    public BaseSoundSettings SparceSoundSettings;
    public float SparceDensity;

    [Header("SpawnInfo")]
    public List<MobSpawnInfo> NaturalMobs;

    public bool MatchCondition(float moisture, float heat)
    {
        return moisture >= minMoisture && heat >= minHeat;
    }

    public float GetDiffValue(float moisture, float heat)
    {
        return (moisture - minMoisture) + (heat - minHeat);
    }

    public void SetSparce(Vector2Int worldPos, System.Random rand, BlockSlice slice)
    {
        if (SparceSoundSettings is not null && SparceBlocks.Any() &&
                 Mathf.Pow(SparceDensity * SparceSoundSettings.GetSound(worldPos.x, worldPos.y), 1.7f) > rand.NextDouble())
        {
            var sparce = SparceBlocks.SelectRandom(rand);
            if (replaceSolid || (sparce is Ground && slice.GroundBlock is null) || (sparce is Wall && slice.WallBlock is null))
            {
                slice.SetBlock(sparce);
            }
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
}
