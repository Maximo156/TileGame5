using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

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
}
