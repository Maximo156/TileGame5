using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBiomePreset", menuName = "Terrain/Biome/BiomePreset", order = 1)]
public class BiomePreset : ScriptableObject
{
    [Header("Gen Info")]
    public float minHeight;
    public float minMoisture;
    public float minHeat;

    [Header("Block Info")]
    public Ground GroundBlock;
    public Wall WallBlock;
    public Roof RoofBlock;

    [Header("Sparce Blocks")]
    public bool replaceSolid = false;
    public List<Block> SparceBlocks;
    public BaseSoundSettings SparceSoundSettings;
    public float SparceDensity;

    public bool MatchCondition(float height, float moisture, float heat)
    {
        return height >= minHeight && moisture >= minMoisture && heat >= minHeat;
    }

    public float GetDiffValue(float height, float moisture, float heat)
    {
        return (height - minHeight) + (moisture - minMoisture) + (heat - minHeat);
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
}
