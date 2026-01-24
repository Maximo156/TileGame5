using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public abstract class BaseSoundSettings : ScriptableObject
{
    public abstract float GetSound(int x, int y);

    public abstract float[,] GetSoundArray(int x, int y, int chunkWidth);

    public abstract JobHandle ScheduleSoundJob(NativeArray<int2> chunks, NativeArray<float> sound, int chunkWidth, JobHandle dep = default);
}
