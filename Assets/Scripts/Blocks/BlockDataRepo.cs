using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace BlockDataRepos
{
    public class BlockDataRepo : MonoBehaviour
    {
        public static NativeBlockDataRepo NativeRepo;
        public static Dictionary<ushort, Block> Blocks;

        private void Awake()
        {
            var tmp = Resources.LoadAll<Block>("ScriptableObjects/Blocks").OrderBy(b => b.name).ToList();
            tmp.Add(ProxyBlock.Instance);
            Debug.Log($"Loading {tmp.Count} blocks");
            Blocks = new Dictionary<ushort, Block>();
            NativeRepo = new(tmp.Count, tmp.Where(b => b is Wall).Sum(b => (b as Wall).MustBePlacedOn.Count));
            ushort count = 1;
            int placedOnCount = 0;
            foreach(var block in tmp)
            {
                block.Id = count;
                count++;
            }
            foreach (var block in tmp)
            {
                Blocks.Add(block.Id, block);
                var data = block.GetBlockData();

                if(block is Wall wall)
                {
                    data.placedOnSlice = new()
                    {
                        start = placedOnCount,
                        length = wall.MustBePlacedOn.Count
                    };
                    foreach(var b in wall.MustBePlacedOn)
                    {
                        NativeRepo.AddMustBePlacedOn(placedOnCount, b.Id);
                        placedOnCount++;
                    }
                }

                NativeRepo.Data.Add(block.Id, data); 
            }
        }

        private void OnDestroy()
        {
            NativeRepo.Dispose();
        }

        public static T GetBlock<T>(ushort id) where T : Block
        {
            return id == 0 ? null : Blocks[id] as T;
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
            if(id != 0 && Blocks[id] is T b)
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
        public NativeHashMap<ushort, BlockData> Data;
        NativeArray<ushort> mustBePlacedOn;

        public NativeBlockDataRepo(int count, int placedOnCount)
        {
            Data = new NativeHashMap<ushort, BlockData>(count, Allocator.Persistent);
            mustBePlacedOn = new NativeArray<ushort>(placedOnCount, Allocator.Persistent);
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
            return id == 0 ? default : Data[id];
        }

        public bool TryGetBlock(ushort id, out BlockData blockData)
        {
            return Data.TryGetValue(id, out blockData);
        }

        public NativeSlice<ushort> GetMustBePlacedOn(BlockData block)
        {
            var slice = block.placedOnSlice;
            return mustBePlacedOn.Slice(slice.start, slice.length);
        }
    }

    public struct BlockData 
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

        public SliceData placedOnSlice;
    }

    public enum BlockLevel
    {
        None = 0,
        Floor,
        Wall,
        Roof
    }
}
