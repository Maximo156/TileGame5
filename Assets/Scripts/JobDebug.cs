using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class JobDebug : MonoBehaviour
{
    public static NativeList<int2> closed;
    public static NativeList<int2> open;
    public static NativeList<BoundsInt> bounds;

    private void Awake()
    {
        closed = new NativeList<int2>(0, Allocator.Persistent);
        open = new NativeList<int2>(0, Allocator.Persistent);
        bounds = new NativeList<BoundsInt>(0, Allocator.Persistent);
    }

    private void OnDisable()
    {
        closed.Dispose();
        open.Dispose();
        bounds.Dispose();
    }

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying || !closed.IsCreated) return;

        Gizmos.color = Color.red;
        foreach(var pos in closed)
        {
            Gizmos.DrawWireCube(pos.ToVector().ToVector3Int() + Vector3.one * 0.5f, Vector3.one);
        }

        Gizmos.color = Color.green;
        foreach (var pos in open)
        {
            Gizmos.DrawWireCube(pos.ToVector().ToVector3Int() + Vector3.one * 0.5f, Vector3.one);
        }

        Gizmos.color = Color.yellow;
        foreach (var b in bounds)
        {
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
