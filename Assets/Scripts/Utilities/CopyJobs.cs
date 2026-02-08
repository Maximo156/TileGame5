using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public partial struct ArrayCopyJob<T> : IJob where T : unmanaged
{
    [ReadOnly]
    public NativeArray<T> src;

    [WriteOnly]
    public NativeArray<T> dest;
    public void Execute()
    {
        dest.CopyFrom(src);
    }
}

[BurstCompile]
public partial struct SliceCopyJob<T> : IJob where T : unmanaged
{
    [ReadOnly]
    public NativeSlice<T> src;

    [WriteOnly]
    public NativeSlice<T> dest;
    public void Execute()
    {
        dest.CopyFrom(src);
    }
}
