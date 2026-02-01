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
    public List<ChunkSubGenerator> Generators;
    public Gradient ShadowColor;
    public string Identifier { get; private set; }
    ChunkSaver Saver;

    GenerationStep BaseStep;

    public (RealmData, JobHandle) GetGenJob(int chunkWidth, NativeList<int2> chunks, RealmInfo realmInfo)
    {
        if (BaseStep == null) throw new Exception("Missing base generation step");
        var requestChunks = new NativeList<int2>(chunks.Length, Allocator.Persistent);
        requestChunks.CopyFrom(chunks);
        var (realmData, genDep, biomeData) = BaseStep.Generate(chunkWidth, chunks, requestChunks, realmInfo);
        return (realmData, JobHandle.CombineDependencies(biomeData.Dispose(genDep), requestChunks.Dispose(genDep)));
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
        NativeList<int2> originalChunks,
        NativeList<int2> requestChunks,
        RealmInfo realmInfo)
        {
            Generator.UpdateRequestedChunks(requestChunks, realmInfo);
            var (realmData, dep, biomeData) = 
                Dependency != null ? 
                    Dependency.Generate(chunkWidth, originalChunks, requestChunks, realmInfo) : 
                    (new RealmData(chunkWidth, requestChunks.Length), default(JobHandle), new BiomeData(requestChunks.Length, chunkWidth));
            var genDep = Generator.ScheduleGeneration(chunkWidth, originalChunks.AsArray(), requestChunks.AsArray(), realmData, realmInfo, ref biomeData, dep);
            return (realmData, genDep, biomeData);
        }
    }
}
