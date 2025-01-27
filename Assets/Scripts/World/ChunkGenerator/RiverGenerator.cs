using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRiverGenerator", menuName = "Terrain/RiverGenerator", order = 1)]
public class RiverGenerator : ChunkSubGenerator
{
    public BaseSoundSettings RiverSound;
    public BaseSoundSettings Reducer;
    public float RiverCuttoff;
    public override Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache)
    {
        var riverArray = RiverSound.GetSoundArray(WorldPosition.x, WorldPosition.y, blocks.GetLength(0));
        var reducerArray = Reducer.GetSoundArray(WorldPosition.x, WorldPosition.y, blocks.GetLength(0));
        for(int x = 0; x < blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(0); y++)
            {
                if (biomeInfo.IsWallBiome(cache.HeightMap[x, y])) continue;
                var riverSound = (1 - MathF.Abs(riverArray[x, y] - 0.5f)) - (reducerArray[x, y] * RiverCuttoff);
                if(riverSound > 1-RiverCuttoff)
                {
                    blocks[x, y] = new BlockSlice();
                }
            }
        }
        return Task.CompletedTask;
    }
}
