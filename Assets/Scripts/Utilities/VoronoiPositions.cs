using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VoronoiPositions
{
    public int Length;
    NativeArray<int2> WorldPositions;

    int2 OriginChunk;
    int ChunkWidth;
    public int DataWidth;

    public VoronoiPositions(int2 originChunk, int chunkWidth, int dataWidth, uint seed, Allocator allocator = Allocator.Persistent)
    {
        OriginChunk = originChunk;
        ChunkWidth = chunkWidth;
        DataWidth = dataWidth;

        Length = dataWidth * dataWidth;

        WorldPositions = new NativeArray<int2>(Length, allocator);
        InitPositions(dataWidth, seed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void InitPositions(int dataWidth, uint seed)
    {
        
        for (int x = 0; x < dataWidth; x++)
        {
            for (int y = 0; y < dataWidth; y++)
            {
                var worldChunk = math.int2(x, y) + OriginChunk;
                var random = new Unity.Mathematics.Random(seed ^ (uint)worldChunk.GetHashCode());
                var pos = random.NextInt2(ChunkWidth);
                WorldPositions.SetElement2d(x, y, dataWidth, pos + ChunkWidth * worldChunk);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int2 GetClosestPosition(int2 worldPos)
    {
        var chunk = Utilities.GetChunk(worldPos, ChunkWidth) - OriginChunk;
        if (math.any(chunk < 1) || math.any(chunk <= DataWidth - 1))
        {
            throw new Exception($"worldPos: {worldPos} is out of ounds");
        }

        var closestPoint = math.int2(0);
        var closestDist = float.MinValue;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var c = chunk + math.int2(x, y);
                var point = WorldPositions.GetElement2d(c.x, c.y, DataWidth);
                var dist = math.distancesq(worldPos, point);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPoint = point;
                }
            }
        }

        return closestPoint;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int2 pos, int index) GetClosestPositionAndIndex(int2 worldPos)
    {
        var chunk = Utilities.GetChunk(worldPos, ChunkWidth) - OriginChunk;
        if (math.any(chunk < 1) || math.any(chunk >= DataWidth - 1))
        {
            throw new Exception($"worldPos: {worldPos} is out of bounds");
        }

        var closestPoint = math.int2(0);
        var closestDist = float.MaxValue;
        var closestIndex = -1;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var c = chunk + math.int2(x, y);
                var point = WorldPositions.GetElement2d(c.x, c.y, DataWidth);


                var dist = math.distancesq(worldPos, point);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPoint = point;
                    closestIndex = c.x * DataWidth + c.y;
                }
            }
        }
        return (closestPoint, closestIndex);
    }

    public int2 GetWorldPosition(int i)
    {
        return WorldPositions[i];
    }

    public void Dispose()
    {
        WorldPositions.Dispose();
    }

    public JobHandle Dispose(JobHandle dep)
    {
        return WorldPositions.Dispose(dep);
    }
}
