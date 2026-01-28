using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

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
