using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public abstract class BaseSoundSettings : ScriptableObject
{
    public Vector2Int Offset => Utilities.SeededVector2Int(4000, (uint)(name.GetHashCode() ^ WorldSave.ActiveSeed));

    public abstract float GetSound(int x, int y);

    public abstract float[,] GetSoundArray(int x, int y, int chunkWidth);

    public abstract JobHandle ScheduleSoundJob(NativeArray<int2> chunks, NativeArray<float> sound, int chunkWidth, JobHandle dep = default);
}
