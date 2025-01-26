using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBiomeInfo", menuName = "Terrain/Biome/BiomeInfo", order = 1)]
public class BiomeInfo : ScriptableObject
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

    public void UpdateBlockSlices(Vector2Int worldPos, BlockSlice[,] blocks, System.Random rand)
    {
        int chunkWidth = blocks.GetLength(0);
        var heightMap = HeightSound.GetSoundArray(worldPos.x, worldPos.y, chunkWidth);
        var moistureMap = MoistureSound?.GetSoundArray(worldPos.x, worldPos.y, chunkWidth);
        var heatMap = HeatSound?.GetSoundArray(worldPos.x, worldPos.y, chunkWidth);

        for(int x = 0; x< blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(1); y++)
            {
                var wallBiome = GetWall(heightMap[x, y]);
                var biome = GetBiome(heightMap[x, y], moistureMap?[x, y] ?? 0, heatMap?[x, y] ?? 0);

                var block = new BlockSlice();
                blocks[x, y] = block;
                if(biome is null)
                {
                    continue;
                }

                block.Water = false;

                if (wallBiome is null)
                {
                    block.SetBlock(biome.GroundBlock);
                    biome.SetSparce(worldPos, rand, block);
                }
                else
                {
                    block.SetBlock(biome.GetReplacement(wallBiome.GroundBlock));
                    block.SetBlock(biome.GetReplacement(wallBiome.WallBlock));
                    block.SetBlock(biome.GetReplacement(wallBiome.RoofBlock));
                    wallBiome.SetSparce(worldPos, rand, block);
                }
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

    public void OnEnable()
    {
        WallBiomes = WallBiomes.OrderBy(w => w.minHeight).ToList();
    }
}
