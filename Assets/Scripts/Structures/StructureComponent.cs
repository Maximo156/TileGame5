using NativeRealm;
using Newtonsoft.Json;
using System;
using Unity.Collections;
using UnityEngine;

public struct StructureComponent
{
    public Vector2Int size;

    public ushort[] Ground;
    public ushort[] Walls;
    public ushort[] Roof;
    public byte[] SimpleStates;

    [JsonIgnore]
    public bool IsCreated;

    public StructureComponent(Vector2Int size)
    {
        IsCreated = true;
        this.size = size;
        var length = size.x * size.y;
        Ground = new ushort[length];
        Walls = new ushort[length];
        Roof = new ushort[length];
        SimpleStates = new byte[length];
    }

    public void SetSlice(NativeBlockSlice slice, int i)
    {
        Ground[i] = slice.groundBlock;
        Walls[i] = slice.wallBlock;
        Roof[i] = slice.roofBlock;
        SimpleStates[i] = slice.simpleBlockState;
    }

    public NativeBlockSlice GetSlice(int i)
    {
        return new()
        {
            groundBlock = Ground[i],
            wallBlock = Walls[i],
            roofBlock = Roof[i],
            simpleBlockState = SimpleStates[i],
        };
    }
}
