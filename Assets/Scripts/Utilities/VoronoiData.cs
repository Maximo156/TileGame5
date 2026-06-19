using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

[BurstCompile]
public struct VoronoiData<T> where T : unmanaged 
{
    public int Width => Positions.DataWidth;
    public int Length => Positions.Length;
    public VoronoiPositions Positions;
    NativeArray<T> Data;

    public VoronoiData(int2 originChunk, int chunkWidth, int dataWidth, uint seed, Allocator allocator = Allocator.Persistent)
    {
        var len = dataWidth * dataWidth;

        Positions = new VoronoiPositions(originChunk, chunkWidth, dataWidth, seed, allocator);
        Data = new NativeArray<T>(len, allocator);
    }

    public VoronoiData(VoronoiPositions positions, Allocator allocator = Allocator.Persistent)
    {
        Positions = positions;
        Data = new NativeArray<T>(Positions.Length, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    (int2 pos, int index) GetClosestPositionAndIndex(int2 worldPos)
    {
        return Positions.GetClosestPositionAndIndex(worldPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(int index, T value)
    {
        Data[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(int x, int y, T value)
    {
        SetValue(x * Width + y, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValue(int2 worldPosition)
    {
        (var _, var index) = GetClosestPositionAndIndex(worldPosition);
        return Data[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int2 pos, T val) GetWorldPositionAndValue(int i)
    {
        return (Positions.GetWorldPosition(i), Data[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int2 pos, T val) GetWorldPositionAndValue(int x, int y)
    {
        return GetWorldPositionAndValue(x * Width + y);
    }

    public void CopyTo(VoronoiData<T> dest) 
    {
        Data.CopyTo(dest.Data);
    }

    public void Dispose(bool dataOnly = false)
    {
        if (!dataOnly)
        {
            Positions.Dispose();
        }

        Data.Dispose();
    }

    public JobHandle Dispose(JobHandle dep)
    {
        return JobHandle.CombineDependencies(Data.Dispose(dep), Positions.Dispose(dep));
    }
}
