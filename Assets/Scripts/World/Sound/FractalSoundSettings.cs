using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

[CreateAssetMenu(fileName = "NewFractalSoundSettings", menuName = "Terrain/FractalSoundSettings", order = 1)]
[BurstCompile]
public class FractalSound : ScriptableObject
{
    public FractalSoundSettings settings;

    public Vector2Int Offset => Utilities.SeededVector2Int(4000, (uint)(name.GetHashCode() ^ WorldSave.ActiveSeed));

    public float GetSound(int x, int y)
    {
        x += Offset.x;
        y += Offset.y;
        var pos = new float2(x, y);
        return GetSound(ref pos, ref settings);
    }

    public JobHandle ScheduleSoundJob(NativeArray<int2> chunks, NativeArray<float> sound, int chunkWidth, JobHandle dep = default)
    {
        return new CalcFractalSoundJob()
        {
            chunks = chunks,
            offset = Offset,
            chunkWidth = chunkWidth,
            settings = settings,
            res = sound,
        }.Schedule(chunks.Length, 1, dep);
    }

    [BurstCompile]
    static float GetSound(ref float2 pos, ref FractalSoundSettings settings)
    {
        pos += new float2(0.001f, 0.007f);
        float value = 0;
        float amplitude = 1;

        float scale = settings.Scale;
        float lacunarity = settings.Lacunarity;
        float persistence = settings.Persistence;

        for (int i = 0; i < settings.Octaves; i++)
        {
            value += noise.cnoise(pos / scale) * amplitude;
            pos *= lacunarity;
            amplitude *= persistence;
        }
        value = value / 2 + 0.5f;
        return math.clamp(value, 0, 1);
    }

    [BurstCompile]
    static void GetSoundArray(ref float2 pos, int chunkWidth, ref FractalSoundSettings settings, ref NativeSlice<float> res)
    {
        for(int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkWidth; y++)
            {
                var p = pos + new float2(x, y);
                res.SetElement2d(x, y, chunkWidth, GetSound(ref p, ref settings));
            } 
        }
    }

    [BurstCompile]
    public partial struct CalcFractalSoundJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int2> chunks;
        public Vector2Int offset;
        public int chunkWidth;
        public FractalSoundSettings settings;
         
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> res;

        public void Execute(int index)
        {
            var chunk = res.GetChunk(index, chunkWidth * chunkWidth);
            var pos = chunks[index] * math.float2(chunkWidth) + math.int2(offset.x, offset.y);
            GetSoundArray(ref pos, chunkWidth, ref settings, ref chunk);
        }
    }

    [Serializable]
    public struct FractalSoundSettings
    {
        [Min(1)]
        public int Octaves;
        [Min(1)]
        public float Lacunarity;
        [Range(0, 1)]
        public float Persistence;
        [Min(1)]
        public float Scale;
    }
}
