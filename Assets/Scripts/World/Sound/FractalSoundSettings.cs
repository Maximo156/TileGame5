using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

[CreateAssetMenu(fileName = "NewFractalSoundSettings", menuName = "Terrain/FractalSoundSettings", order = 1)]
[BurstCompile]
public class FractalSoundSettings : BaseSoundSettings
{
    public int Seed;
    [Min(1)]
    public int Octaves;
    [Min(1)]
    public float Lacunarity;
    [Range(0,1)]
    public float Persistence;
    [Min(1)]
    public float Scale;

    public bool useBurst = true;

    Vector2Int offset;

    public override float GetSound(int x, int y)
    {
        x += offset.x;
        y += offset.y;
        var pos = new float2(x, y);
        return GetSound(ref pos, Octaves, Lacunarity, Persistence, Scale);
    }

    public override float[,] GetSoundArray(int x1, int y1, int chunkWidth)
    {
        x1 += offset.x;
        y1 += offset.y;
        var res = new float[chunkWidth, chunkWidth];
        if (useBurst)
        {
            var pos = new float2(x1, y1);
            var native = new NativeArray<float>(chunkWidth * chunkWidth, Allocator.Persistent);
            GetSoundArray(ref pos, chunkWidth, Octaves, Lacunarity, Persistence, Scale, ref native);
            for (int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    res[x, y] = native[x + y * chunkWidth];
                }
            }
            native.Dispose();
        }
        else
        {
            for (int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    res[x, y] = GetSound(x1 + x, y1+y);
                }
            }
            return res;
        }
        return res;
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
    static void GetSoundArray(ref float2 pos, int chunkWidth, int Octaves, float Lacunarity, float Persistence, float Scale, ref NativeArray<float> res)
    {
        for(int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkWidth; y++)
            {
                var p = pos + new float2(x, y);
                res[x + y * chunkWidth] = GetSound(ref p, Octaves, Lacunarity, Persistence, Scale);
            }
        }
    }

    public void OnValidate()
    {
        var rand = new System.Random(Seed);
        offset = Utilities.RandomVector2Int(4000, rand);
    }
}
