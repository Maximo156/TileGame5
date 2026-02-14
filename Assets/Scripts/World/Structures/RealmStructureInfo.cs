using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[Serializable]
public class RealmStructureInfo
{
    public int StructureChunkWidth;
    public List<Structure> Structures;

    NativeStructureInfo structureInfo;
    public NativeStructureInfo StructureInfo {
        get
        {
            if (!structureInfo.isCreated)
            {
                structureInfo = InitStructInfo();
            }
            return structureInfo;
        }
    }

    public void Dispose()
    {
        if (structureInfo.isCreated)
        {
            structureInfo.Dispose();
        }
    }

    public void AttemptFillStorageState(Vector2Int pos, byte index, IStorageBlockState state)
    {
        var iPos = pos.ToInt();
        var structChunk = Utilities.GetChunk(pos, StructureChunkWidth).ToInt();
        var (closestPoint, _, closestStruct) = GenStructureInfo(structChunk, StructureChunkWidth, StructureInfo, WorldSave.ActiveSeed);
        var minDist = math.distancesq(iPos, closestPoint);
        foreach(var v in Utilities.OctAdjacentInt)
        {
            var (newPoint, _, newStruct) = GenStructureInfo(structChunk + v, StructureChunkWidth, StructureInfo, WorldSave.ActiveSeed);
            var d = math.distancesq(iPos, newPoint);
            if (d < minDist)
            {
                closestPoint = newPoint;
                closestStruct = newStruct;
                minDist = d;
            }
        }
        var structure = Structures[closestStruct];
        if(index >= structure.components.Count)
        {
            throw new Exception("Invalid structure state");
        }
        var lootTable = structure.components[index].GenerateLootEntry(new System.Random(structChunk.GetHashCode() << 10 ^ pos.GetHashCode() ^ (int)WorldSave.ActiveSeed));
        foreach(var i in lootTable)
        {
            state.AddItemStack(i);
        }
    }

    NativeStructureInfo InitStructInfo()
    {
        var centerBounds = Structures.Select(s => s.Centers.Select(c => c.ComputeBounds()).ToList()).ToList();
        var buildingBounds = Structures.Select(s => s.components.Select(c => c.ComputeBounds()).ToList()).ToList();
        var totalBlocks = centerBounds.Sum(s => s.Sum(b => b.bounds.size.x * b.bounds.size.y)) + buildingBounds.Sum(s => s.Sum(b => b.bounds.size.x * b.bounds.size.y));
        var totalAnchors = centerBounds.Sum(s => s.Sum(b => b.anchorCount)) + buildingBounds.Sum(s => s.Sum(b => b.anchorCount));

        var res = new NativeStructureInfo(Structures.Count, Structures.Sum(s => s.components.Count) + Structures.Sum(s => s.Centers.Count), totalBlocks, totalAnchors);

        var componentCounter = 0;
        var blockCounter = 0;
        var anchorCounter = 0;

        for (int i = 0; i<Structures.Count; i++)
        {
            var curStructure = Structures[i];
            var centerCount = curStructure.Centers.Count;
            var buildingsCount = curStructure.components.Count;
            var nativeStructure = new NativeStructure()
            {
                maxComponentCount = curStructure.maxComponentCount,
                minComponentCount = curStructure.minComponentCount,
                CenterComponentsSlice = new() { start = componentCounter, length = centerCount },
                BuildingsComponentSlice = new() { start = componentCounter + centerCount, length = buildingsCount },
            };
            componentCounter += buildingsCount + centerCount;
            res.structures[i] = nativeStructure;

            var StructCenterSlice = res.GetStructureCenterComponents(nativeStructure);
            for(int j = 0; j < curStructure.Centers.Count; j++)
            {
                var curCenter = curStructure.Centers[j];
                var bounds = centerBounds[i][j];
                var blockCount = bounds.bounds.size.x * bounds.bounds.size.y;
                var anchorCount = bounds.anchorCount;
                var nativeCenter = new NativeStructureComponent()
                {
                    Name = curCenter.name,
                    Bounds = bounds.bounds,
                    SelfIndex = j,
                    BlocksSlice = new() { start = blockCounter, length = blockCount },
                    AnchorSlice = new() { start = anchorCounter, length = anchorCount }
                };
                blockCounter += blockCount;
                anchorCounter += anchorCount;
                curCenter.InitializeNativeComponent(ref nativeCenter, res.GetComponentBlocks(nativeCenter), res.GetComponentAnchors(nativeCenter));
                StructCenterSlice[j] = nativeCenter;
            }

            var StructBuildingSlice = res.GetStructureBuildingComponents(nativeStructure);
            for (int j = 0; j < curStructure.components.Count; j++)
            {
                var curBuilding = curStructure.components[j];
                var bounds = buildingBounds[i][j];
                var blockCount = bounds.bounds.size.x * bounds.bounds.size.y; 
                var anchorCount = bounds.anchorCount; 
                var nativeBuilding = new NativeStructureComponent()
                {
                    Name = curBuilding.name,
                    Bounds = bounds.bounds,
                    SelfIndex = j,
                    BlocksSlice = new() { start = blockCounter, length = blockCount },
                    AnchorSlice = new() { start = anchorCounter, length = anchorCount }
                };
                blockCounter += blockCount;
                anchorCounter += anchorCount;
                curBuilding.InitializeNativeComponent(ref nativeBuilding, res.GetComponentBlocks(nativeBuilding), res.GetComponentAnchors(nativeBuilding));
                StructBuildingSlice[j] = nativeBuilding;
            }
        }
        return res;
    }


    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint GetChunkSeed(int2 StructureChunk)
    {
        return (uint)StructureChunk.GetHashCode();
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Random GetChunkRandom(int2 StructureChunk, uint worldSeed)
    {
        return new Random(GetChunkSeed(StructureChunk) ^ worldSeed);
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int2 GenPoint(int2 StructureChunk, int StructureChunkWidth, Random rand)
    {
        return (StructureChunk * StructureChunkWidth) + rand.NextInt2(0, StructureChunkWidth);
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int2 point, Random rand, int structIndex) GenStructureInfo(int2 StructureChunk, int StructureChunkWidth, NativeStructureInfo structInfo, uint worldSeed)
    {
        var rand = GetChunkRandom(StructureChunk, worldSeed);
        var pos = GenPoint(StructureChunk, StructureChunkWidth, rand);
        var structIndex = rand.NextInt(0, structInfo.structures.Length);
        return (pos, rand, structIndex);
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PopulateAdjacentPoints(int2 StructureChunk, int StructureChunkWidth, NativeArray<int2> points, uint worldSeed)
    {
        PopulatePoint(StructureChunk + math.int2(-1, 1), 0);
        PopulatePoint(StructureChunk + math.int2(0, 1), 0);
        PopulatePoint(StructureChunk + math.int2(1, 1), 0);

        PopulatePoint(StructureChunk + math.int2(-1, 0), 0);
        PopulatePoint(StructureChunk + math.int2(1, 0), 0);

        PopulatePoint(StructureChunk + math.int2(-1, -1), 0);
        PopulatePoint(StructureChunk + math.int2(0, -1), 0);
        PopulatePoint(StructureChunk + math.int2(1, -1), 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void PopulatePoint(int2 chunk, int index)
        {
            var rand = GetChunkRandom(chunk, worldSeed);
            var pos = GenPoint(StructureChunk, StructureChunkWidth, rand);
            points[index] = pos;
        }
    }
}
