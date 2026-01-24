using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class GenerationCache
{
    public float[,] HeightMap;
    public float[,] MoistureMap;
    public float[,] HeatMap;
}

public struct BiomeData
{
    public NativeArray<float> HeightMap;
    public NativeArray<float> MoistureMap;
    public NativeArray<float> HeatMap;

    public void Dispose()
    {
        HeightMap.Dispose();
        MoistureMap.Dispose();
        HeatMap.Dispose();
    }

    public JobHandle Dispose(JobHandle handle)
    {
        return JobHandle.CombineDependencies(
            HeightMap.Dispose(handle),
            MoistureMap.Dispose(handle),
            HeatMap.Dispose(handle)
        );
    }
}
