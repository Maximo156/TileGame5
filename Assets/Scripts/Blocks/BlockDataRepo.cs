using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace BlockDataRepos
{
    public class BlockDataRepo : MonoBehaviour
    {
        public static NativeBlockDataRepo NativeRepo;
        static Block[] Blocks;

        private void Awake()
        {
            var tmp = Resources.LoadAll<Block>("ScriptableObjects/Blocks").OrderBy(b => b.name).ToList();
            tmp.Add(ProxyBlock.Instance);
            Debug.Log($"Loading {tmp.Count} blocks");
            Blocks = new Block[tmp.Count];
            ushort count = 1;
            foreach(var block in tmp)
            {
                block.Id = count;
                count++;
            }

            NativeRepo = new(tmp);
            foreach (var block in tmp)
            {
                Blocks[block.Id-1] = block;
                var data = block.GetBlockData();
            }
        }

        private void OnDestroy()
        {
            NativeRepo.Dispose();
        }

        public static T GetBlock<T>(ushort id) where T : Block
        {
            return id == 0 ? null : Blocks[id-1] as T;
        }

        public static BlockData GetNativeBlock(ushort id)
        {
            return NativeRepo.GetBlock(id);
        }

        public static bool TryGetNativeBlock(ushort id, out BlockData data)
        {
            return NativeRepo.TryGetBlock(id, out data);
        }

        public static bool TryGetBlock<T>(ushort id, out T block) where T : Block
        {
            if(id != 0 && Blocks[id - 1] is T b)
            {
                block = b;
                return true;
            }
            block = null;
            return false;
        }

        public static NativeSlice<ushort> GetMustBePlacedOn(BlockData block)
        {
            return NativeRepo.GetMustBePlacedOn(block);
        }
    }

    public struct NativeBlockDataRepo
    {
        NativeArray<BlockData> Data;
        NativeArray<ushort> mustBePlacedOn;

        public NativeBlockDataRepo(List<Block> blocks)
        {
            blocks.ToNativeSlices(
                b => b is Wall wall ? wall.MustBePlacedOn.Select(b => b.Id) : null, 
                b => b.GetBlockData(),
                SetSliceData,
                out mustBePlacedOn,
                out Data);

        }

        public void Add(ushort id, BlockData data)
        {
            Data[id-1] = data;
        }

        public void AddMustBePlacedOn(int index, ushort id)
        {
            mustBePlacedOn[index] = id;
        }

        public void Dispose()
        {
            Data.Dispose();
            mustBePlacedOn.Dispose();
        }

        public BlockData GetBlock(ushort id)
        {
            return id == 0 ? default : Data[id - 1];
        }

        public bool TryGetBlock(ushort id, out BlockData blockData)
        {
            if(id == 0)
            {
                blockData = default;
                return true;
            }
            blockData = Data[id - 1];
            return true;
        }

        public readonly NativeSlice<ushort> GetMustBePlacedOn(BlockData block)
        {
            var slice = block.placedOnSlice;
            return mustBePlacedOn.Slice(slice.start, slice.length);
        }

        static void SetSliceData(ref BlockData data, SliceData d)
        {
            data.sliceData = d;
        }
    }

    public struct BlockData : IHasSliceData
    {
        public BlockLevel Level;
        public byte lightLevel;
        public float movementSpeed;
        public bool walkable;
        public bool structural;
        public bool solid;
        public bool door;
        public bool isProxy;
        public int roofStrength;
        public int hitsToBreak;

        public SliceData placedOnSlice;

        public SliceData sliceData {get => placedOnSlice; set => placedOnSlice = value; }
    }

    public enum BlockLevel
    {
        None = 0,
        Floor,
        Wall,
        Roof
    }
}
