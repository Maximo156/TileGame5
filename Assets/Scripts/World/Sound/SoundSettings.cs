using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

[CreateAssetMenu(fileName = "NewSoundSettings", menuName = "Terrain/SoundSettings", order = 1)]
public class SoundSettings : BaseSoundSettings
{
    public int Seed;
    [Serializable]
    public class Octave
    {
        public float scale;
        public float amplitude;
        public Vector2Int offset { get; set; }
    }

    public Octave World = new Octave();
    public bool useWorld;
    public List<Octave> octaves = new List<Octave>();

    public override float GetSound(int x, int y)
    {
        float res = octaves.Count == 0 ? 1 : 0;
        float ampSum = octaves.Sum(o => o.amplitude);
        foreach (var octave in octaves)
        {
            res += GetSound(x, y, octave) / ampSum;
        }
        return  (useWorld ? GetSound(x, y, World) : 1) * res * 0.5f + 0.5f;
    }

    float GetSound(int x, int y, Octave octave)
    {
        var xSample = (x / octave.scale * (1f / 32f) + octave.offset.x + 0.2f) % 100000;
        var ySample = (y / octave.scale * (1f / 32f) + octave.offset.y + 0.1f) % 100000;
        return octave.amplitude * (Mathf.PerlinNoise(xSample, ySample) * 2 - 1);
    }

    private void OnEnable()
    {
        var rand = new System.Random(Seed);
        foreach(var octave in octaves)
        {
            octave.offset = new Vector2Int(rand.Next(), rand.Next());
        }
        World.offset = new Vector2Int(rand.Next(), rand.Next());
    }

    public override float[,] GetSoundArray(int x1, int y1, int chunkWidth)
    {
        var res = new float[chunkWidth, chunkWidth];
        for (int x = 0; x < res.GetLength(0); x++)
        {
            for (int y = 0; y < res.GetLength(1); y++)
            {
                res[x, y] = GetSound(x1+x, y1+y);
            }
        }
        return res;
    }

    public override JobHandle ScheduleSoundJob(NativeArray<int2> chunks, NativeArray<float> sound, int chunkWidth, JobHandle dep = default)
    {
        throw new NotImplementedException();
    }

    [BurstCompile]
    static float GetSound(ref float2 pos, int Octaves, float Lacunarity, float Persistence, float Scale)
    {
        pos += new float2(0.001f, 0.007f);
        float value = 0;
        float amplitude = 1;

        for (int i = 0; i < Octaves; i++)
        {
            value += noise.cnoise(pos / Scale) * amplitude;
            pos *= Lacunarity;
            amplitude *= Persistence;
        }
        value = value / 2 + 0.5f;
        return math.clamp(value, 0, 1);
    }

    [BurstCompile]
    static void GetSoundArray(ref float2 pos, int chunkWidth, int Octaves, float Lacunarity, float Persistence, float Scale, ref NativeSlice<float> res)
    {
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkWidth; y++)
            {
                var p = pos + new float2(x, y);
                res.SetElement2d(x, y, chunkWidth, GetSound(ref p, Octaves, Lacunarity, Persistence, Scale));
            }
        }
    }
}
