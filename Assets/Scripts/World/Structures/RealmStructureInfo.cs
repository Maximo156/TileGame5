using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class RealmStructureInfo
{
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

    NativeStructureInfo InitStructInfo()
    {
        var centerBounds = Structures.Select(s => s.Centers.Select(c => c.ComputeBounds()).ToList()).ToList();
        var buildingBounds = Structures.Select(s => s.components.Select(c => c.ComputeBounds()).ToList()).ToList();
        var totalBlocks = centerBounds.Sum(s => s.Sum(b => b.size.x * b.size.y)) + buildingBounds.Sum(s => s.Sum(b => b.size.x * b.size.y));

        var res = new NativeStructureInfo(Structures.Count, Structures.Sum(s => s.components.Count), totalBlocks);

        var componentCounter = 0;
        var blockCounter = 0;

        for(int i = 0; i<Structures.Count; i++)
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
                var blockCount = bounds.size.x * bounds.size.y;
                var nativeCenter = new NativeStructureComponent()
                {
                    BlocksSlice = new() { start = blockCounter, length = blockCount }
                };
                blockCount += blockCount;
                curCenter.InitializeNativeComponent(ref nativeCenter, res.GetComponentBlocks(nativeCenter), res.GetComponentAnchors(nativeCenter));
            }

            var StructBuildingSlice = res.GetStructureBuildingComponents(nativeStructure);
            for (int j = 0; j < curStructure.Centers.Count; j++)
            {
                var curCenter = curStructure.components[j];
                var bounds = buildingBounds[i][j];
                var blockCount = bounds.size.x * bounds.size.y;
                var nativeCenter = new NativeStructureComponent()
                {
                    BlocksSlice = new() { start = blockCounter, length = blockCount }
                };
                blockCounter += blockCount;
                curCenter.InitializeNativeComponent(ref nativeCenter, res.GetComponentBlocks(nativeCenter), res.GetComponentAnchors(nativeCenter));
            }
        }


        return res;
    }
}
