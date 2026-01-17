using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using NativeRealm;

[CreateAssetMenu(fileName = "NewChunkGenerator", menuName = "Terrain/ChunkGenerator", order = 1)]
public class ChunkGenerator: ScriptableObject, ISaveable
{
    public bool saveChunks;
    public BiomeInfo biomes;
    public List<ChunkSubGenerator> Generators;
    public Gradient ShadowColor;
    public string Identifier { get; private set; }
    ChunkSaver Saver;

    public async Task<BlockSliceState[,]> GetBlockSlices(Vector2Int ChunkPosition, Vector2Int WorldPosition, int chunkWidth, System.Random rand, ChunkData chunkData)
    {
        var blocksStates = new BlockSliceState[chunkWidth, chunkWidth];
        if (saveChunks && Saver.TryLoadBlockSlices(ChunkPosition, out blocksStates))
        {
            throw new NotImplementedException();
            //return blocks;
        }
        var cache = new GenerationCache();
        foreach (var generator in Generators)
        {
            await generator.UpdateBlockSlices(blocksStates, chunkData, ChunkPosition, WorldPosition, biomes, rand, cache);
        }
        return blocksStates;
    }

    public void SaveChunk(Chunk chunk)
    {
        Saver.SaveChunk(chunk);
    }

    public Color GetColor(int hoursPerDay, float curTime)
    {
        var val = curTime / hoursPerDay;
        val = val < 0.5 ? val : 1 - val;
        return ShadowColor.Evaluate(val * 2);
    }

    void OnValidate()
    {
        Generators = Generators.OrderBy(g => g.Priority).ToList();
        Identifier = name;
        Saver = new ChunkSaver(name);
    }
}
