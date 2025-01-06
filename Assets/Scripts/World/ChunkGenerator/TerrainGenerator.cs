using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[CreateAssetMenu(fileName = "NewTerrainGenerator", menuName = "Terrain/Generator", order = 1)]
public class TerrainGenerator : ChunkSubGenerator
{
    public override Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand)
    {
        for(int x = 0; x< blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(0); y++)
            {
                var worldPos = new Vector2Int(x, y) + WorldPosition;
                blocks[x, y] = GenerateBlock(worldPos, biomeInfo.GetInfo(worldPos), rand);
            }
        }
        return Task.CompletedTask;
    }

    BlockSlice GenerateBlock(Vector2Int worldPos, BiomeInfo.BiomeBlockInfo biomeInfo, System.Random rand)
    {
        var slice = new BlockSlice();
        slice.Water = biomeInfo.Water;
        var layer = biomeInfo.layer;
        if (layer != null)
        {
            slice.SetBlock(layer.GroundBlocks.SelectRandom(rand));
            slice.SetBlock(layer.WallBlocks.SelectRandom(rand));
            if (layer.SparceSoundSettings is not null && layer.SparceBlocks.Any() &&
                 Mathf.Pow(layer.SparceDensity * layer.SparceSoundSettings.GetSound(worldPos.x, worldPos.y), 1.7f) > rand.NextDouble())
            {
                var sparce = layer.SparceBlocks.SelectRandom(rand);
                if (layer.replaceSolid || (sparce is Ground && slice.GroundBlock is null) || (sparce is Wall && slice.WallBlock is null))
                {
                    slice.SetBlock(sparce);
                }
            }
        }
        return slice;
    }

}
