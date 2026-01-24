using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using NativeRealm;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

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

    GenerationStep BaseStep;

    public (RealmData, JobHandle) GetGenJob(int chunkWidth, NativeList<int2> chunks)
    {
        if (BaseStep == null) throw new Exception("Missing base generation step");
        var (realmData, genDep, biomeData) = BaseStep.Generate(chunkWidth, chunks, biomes);
        return (realmData, biomeData.Dispose(genDep));
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
        BaseStep = null;
        foreach (var generator in Generators)
        {
            BaseStep = new GenerationStep(generator, BaseStep);
        }

        Identifier = name;
        Saver = new ChunkSaver(name);
    }

    public class GenerationStep
    {
        readonly ChunkSubGenerator Generator;
        readonly GenerationStep Dependency;

        public GenerationStep(ChunkSubGenerator Generator, GenerationStep Dependency)
        {
            this.Generator = Generator;
            this.Dependency = Dependency;
        }

        public (RealmData, JobHandle, BiomeData) Generate(int chunkWidth,
        NativeList<int2> chunks,
        BiomeInfo biomeInfo)
        {
            Debug.Log($"Running {Generator.name}");
            Generator.UpdateRequestedChunks(chunks);
            var (realmData, dep, biomeData) = 
                Dependency != null ? 
                    Dependency.Generate(chunkWidth, chunks, biomeInfo) : 
                    (new RealmData(chunkWidth, chunks.Length), default(JobHandle), default(BiomeData));
            var genDep = Generator.ScheduleGeneration(chunkWidth, chunks.AsArray(), realmData, biomeInfo, ref biomeData, dep);
            return (realmData, genDep, biomeData);
        }
    }
}
