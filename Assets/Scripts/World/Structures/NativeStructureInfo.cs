using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public struct NativeStructureInfo
{
    NativeArray<NativeComponentBlockSlice> blockSlices;
    NativeArray<NativeComponentAnchor> anchors;

    public NativeArray<NativeStructure> structures;
    NativeArray<NativeStructureComponent> components;

    public NativeStructureInfo(int structureCount, int componentsCount, int blockCount, int anchor)
    {
        blockSlices = new(blockCount, Allocator.Persistent);
        anchors = new(anchor, Allocator.Persistent);

        structures = new(structureCount, Allocator.Persistent);
        components = new(componentsCount, Allocator.Persistent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeSlice<NativeStructureComponent> GetStructureCenterComponents(NativeStructure structure)
    {
        var slice = structure.CenterComponentsSlice;
        return new(components, slice.start, slice.length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeSlice<NativeStructureComponent> GetStructureBuildingComponents(NativeStructure structure)
    {
        var slice = structure.BuildingsComponentSlice;
        return new(components, slice.start, slice.length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeSlice<NativeComponentBlockSlice> GetComponentBlocks(NativeStructureComponent component)
    {
        var slice = component.BlocksSlice;
        return new(blockSlices, slice.start, slice.length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeSlice<NativeComponentAnchor> GetComponentAnchors(NativeStructureComponent component)
    {
        var slice = component.AnchorSlice;
        return new(anchors, slice.start, slice.length);
    }


    public bool isCreated => blockSlices.IsCreated && anchors.IsCreated && components.IsCreated && structures.IsCreated;

    public void Dispose()
    {
        blockSlices.Dispose();
        components.Dispose();
        structures.Dispose();
        anchors.Dispose();
    }
}
