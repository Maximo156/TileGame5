using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;
using System.Runtime.CompilerServices;

[CreateAssetMenu(fileName = "NewFractalSoundSettings", menuName = "Terrain/FractalSoundSettings", order = 1)]
[BurstCompile]
public class FractalSound : ScriptableObject
{
    static readonly float2 perlinOffset = new float2(0.001f, 0.007f);
     
    public FractalSoundSettings settings;
    public FractalSoundSettings GetSettings()
    {
        var s = settings;
        s.Offset = Offset;
        return s;
    }

    public int2 Offset => Utilities.SeededInt2(4000, (uint)(name.GetHashCode() ^ WorldSave.ActiveSeed));

    public float GetSound(int x, int y)
    {
        x += Offset.x;
        y += Offset.y;
        var pos = new float2(x, y);
        var settings = GetSettings();
        return GetSound(ref pos, ref settings);
    }

    public JobHandle ScheduleSoundJob(NativeArray<int2> chunks, NativeArray<float> sound, int chunkWidth, JobHandle dep = default)
    {
        return new CalcFractalSoundJob()
        {
            chunks = chunks,
            chunkWidth = chunkWidth,
            settings = GetSettings(),
            res = sound,
        }.Schedule(chunks.Length, 1, dep);
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetSound(ref float2 origPos, ref FractalSoundSettings settings)
    {
        float2 pos = origPos + settings.Offset + perlinOffset;

        float value = 0f;
        float amplitude = 1f;

        float scale = settings.Scale;
        float lacunarity = settings.Lacunarity;
        float persistence = settings.Persistence;
        int octaves = settings.Octaves;

        for (int octave = 0; octave < octaves; octave++)
        {
            value += noise.cnoise(pos / scale) * amplitude;

            pos *= lacunarity;
            amplitude *= persistence;
        }

        return math.saturate(value * 0.5f + 0.5f);
    }

    [BurstCompile]
    static void GetSoundArray(
    ref float2 pos,
    int chunkWidth,
    ref FractalSoundSettings settings,
    ref NativeSlice<float> res)
    {
        float2 basePos = pos + settings.Offset + perlinOffset;

        float baseScale = settings.Scale;
        float lacunarity = settings.Lacunarity;
        float persistence = settings.Persistence;
        int octaves = settings.Octaves;

        int index = 0;
        float2 samplePos = basePos;
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkWidth; y++)
            {
                float value = 0f;
                float amplitude = 1f;
                float scale = baseScale;

                for (int octave = 0; octave < octaves; octave++)
                {
                    value += noise.cnoise(samplePos / scale) * amplitude;

                    samplePos *= lacunarity;
                    amplitude *= persistence;
                }

                res[index++] = math.saturate(value * 0.5f + 0.5f);

                samplePos.y += 1f;
            }
            samplePos.x += 1f;
        }
    }

    [BurstCompile]
    static void GetSoundArraySimple(
    ref float2 pos,
    int chunkWidth,
    ref FractalSoundSettings settings,
    ref NativeSlice<float> res)
    {
        float2 basePos = pos + settings.Offset + perlinOffset;
        float invScale = 1.0f / settings.Scale;

        int index = 0;
        float2 samplePos = basePos;
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkWidth; y++)
            {
                float value = noise.cnoise(samplePos * invScale);
                res[index++] = math.saturate(value * 0.5f + 0.5f);
                samplePos.y += 1f;
            }
            samplePos.x += 1f;
        }
    }

    [BurstCompile]
    public partial struct CalcFractalSoundJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int2> chunks;
        public int chunkWidth;
        public FractalSoundSettings settings;
         
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float> res;

        public void Execute(int index)
        {
            var chunk = res.GetChunk(index, chunkWidth * chunkWidth);
            var pos = chunks[index] * math.float2(chunkWidth);
            if (settings.Octaves < 2)
            {
                GetSoundArraySimple(ref pos, chunkWidth, ref settings, ref chunk);
            }
            else
            {
                GetSoundArray(ref pos, chunkWidth, ref settings, ref chunk);
            }
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

        [HideInInspector]
        public int2 Offset;
    }
}
