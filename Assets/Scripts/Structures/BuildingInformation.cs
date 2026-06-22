using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using ComposableBlocks;
using Newtonsoft.Json;
using BlockDataRepos;

[CreateAssetMenu(fileName = "NewStructureComponent", menuName = "Terrain/StructureComponent", order = 1)]
public class BuildingInformation : ScriptableObject
{
    [Serializable]
    public class LootTableEntry 
    {
        public float chance;
        public int min;
        public int max;
        public Item Item;
        public List<ItemStack> ItemFill;
    }

    public TextAsset Json;
    public StructureComponent _component;

    public StructureComponent Component 
    {  
        get 
        {
            if (!_component.IsCreated)
            {
                _component = JsonConvert.DeserializeObject<StructureComponent>(Json.text);
            }
            _component.IsCreated = true;
            return _component; 
        } 
    }



    public List<LootTableEntry> lootTable = new List<LootTableEntry>();
    public int Importance;
    public bool AllowMountains;

    public List<ItemStack> GenerateLootEntry(System.Random rand)
    {
        var res = new List<ItemStack>();
        foreach (var entry in lootTable)
        {
            if(rand.NextDouble() < entry.chance)
            {
                var item = new ItemStack(entry.Item, rand.Next(entry.min, entry.max));
                if(item.GetState<ItemInventoryBehaviourState>(out var invState))
                {
                    foreach (var i in entry.ItemFill) {
                        invState.inv.AddItem(new ItemStack(i));
                    }
                }
                res.Add(item);
            }
        }
        return res;
    }

    List<ushort> AnchorBlockIds => BlockDataRepo.GetAllBlocks().Where(b => b.TryGetBehavior<AnchorBlockBehaviour>(out var _)).Select(b => b.Id).ToList();

    public (BoundsInt bounds, int anchorCount) ComputeBounds()
    {
        var anchorIds = AnchorBlockIds;
        var bounds = new BoundsInt(Vector3Int.zero, Component.size.ToVector3Int(1)); 
        var anchorCount = Component.Walls.Count(b => AnchorBlockIds.Contains(b));
        return (bounds, anchorCount);
    }

    public void InitializeNativeComponent(ref NativeStructureComponent targetComponent, NativeSlice<NativeComponentBlockSlice> blocks, NativeSlice<NativeComponentAnchor> anchors)
    {
        var anchorIds = AnchorBlockIds;
        var bounds = targetComponent.Bounds;
        int c = 0;
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                var index = x + y * bounds.size.x;
                var slice = Component.GetSlice(index);
                var isAnchor = anchorIds.Contains(slice.wallBlock);
                blocks.SetElement2d(x, y, bounds.size.y, new NativeComponentBlockSlice()
                {
                    groundBlock = slice.groundBlock,
                    wallBlock = isAnchor ? (ushort)0 : slice.wallBlock,
                    roofBlock = slice.roofBlock,
                    simpleState = slice.simpleBlockState
                });

                if(isAnchor)
                {
                    anchors[c++] = ProcessAnchor(bounds, slice.simpleBlockState, index);
                }
            }
        }

        targetComponent.AllowMountains = AllowMountains;
        targetComponent.Importance = (int)MathF.Max(1, Importance);
    }

    private NativeComponentAnchor ProcessAnchor(BoundsInt bounds, byte simpleState, int index)
    {
        (var code, var key, var dir) = AnchorBlockBehaviour.DecodeState(simpleState);
        var x = index % bounds.size.x;
        var y = index / bounds.size.x;
        var anchorInfo = new NativeComponentAnchor()
        {
            direction = dir,
            offset = new int2(x, y),
            key = code,
            Lock = !key
        };
        return anchorInfo;
    }
}
