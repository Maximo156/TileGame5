using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public enum NodeState : byte
{
    Unvisited,
    Open,
    Closed
}

public struct SearchNode
{
    public float G;
    public ushort Parent;      // Index into the search buffer ushort.MaxValue = none
    public NodeState State;
}

public struct SearchBuffer : IDisposable
{
    public readonly int MaxDistance;
    public readonly int Width;

    public int2 Origin;

    public NativeArray<SearchNode> Nodes;
    public NativeArray<SliceMoveInfo> MoveInfo;
    public NativeArray<byte> HasMove;

    public SearchBuffer(int maxDistance, Allocator allocator)
    {
        MaxDistance = maxDistance;
        Width = maxDistance * 2 + 1;

        Origin = int2.zero;

        Nodes = new NativeArray<SearchNode>(
            Width * Width,
            allocator,
            NativeArrayOptions.ClearMemory);

        HasMove = new NativeArray<byte>(Width * Width, allocator, NativeArrayOptions.ClearMemory);
        MoveInfo = new NativeArray<SliceMoveInfo>(Width * Width, allocator, NativeArrayOptions.UninitializedMemory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(int2 start)
    {
        Origin = start - new int2(MaxDistance);

        for (int i = 0; i < Nodes.Length; i++)
        {
            Nodes[i] = new SearchNode
            {
                G = float.MaxValue,
                Parent = ushort.MaxValue,
                State = NodeState.Unvisited
            };

            HasMove[i] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int2 worldPos)
    {
        int2 local = worldPos - Origin;

        return
            (uint)local.x < (uint)Width &&
            (uint)local.y < (uint)Width;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PositionToIndex(int2 worldPos)
    {
        int2 local = worldPos - Origin;
        return local.x + local.y * Width;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int2 IndexToPosition(int index)
    {
        return Origin + new int2(
            index % Width,
            index / Width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SearchNode Get(int index)
    {
        return ref Nodes.ElementAt(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SearchNode Get(int2 worldPos)
    {
        return ref Nodes.ElementAt(PositionToIndex(worldPos));
    }

    public void Dispose()
    {
        if (Nodes.IsCreated)
            Nodes.Dispose();
    }

    public JobHandle Dispose(JobHandle dep)
    {
        if (!Nodes.IsCreated)
            return default;

        return JobHandle.CombineDependencies(Nodes.Dispose(dep), HasMove.Dispose(dep), MoveInfo.Dispose(dep));
    }
}
