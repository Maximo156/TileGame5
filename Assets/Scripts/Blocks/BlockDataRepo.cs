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
    }

    public struct NativeBlockDataRepo
    {
        public ushort ProxyId { get; private set; }
        NativeArray<TickInfo> tickInfo;
        NativeArray<byte> lightLevels;
        NativeArray<BlockMovementInfo> moveInfo;
        NativeArray<bool> solid;
        NativeArray<bool> lootable;

        public NativeBlockDataRepo(List<Block> blocks)
        {
            ProxyId = blocks.First(b => b is ProxyBlock).Id;
            tickInfo = new(blocks.Count, Allocator.Persistent);
            lightLevels = new (blocks.Count, Allocator.Persistent);
            moveInfo = new (blocks.Count, Allocator.Persistent);
            solid = new (blocks.Count, Allocator.Persistent);
            lootable = new (blocks.Count, Allocator.Persistent);

            for (int i = 0; i < blocks.Count; i++)
            {
                ProcessBlock(blocks[i], i);
            }
        }

        public void ProcessBlock(Block block, int index)
        {
            var wall = block as Wall;
            lightLevels[index] = block is LightBlock light ? (byte)(light.LightLevel * 2) : (byte)0;
            moveInfo[index] = new BlockMovementInfo 
            { 
                door = block is Door,
                movementSpeed = block.MovementModifier,
                walkable = wall?.Walkable ?? true,
            };
            solid[index] = wall?.solid ?? false;
            lootable[index] = block is CrateBlock || block is StorageBlock;
            tickInfo[index] = block is ISimpleTickBlock tick ? tick.GetTickInfo() : default;
        }

        public void Dispose()
        {
            tickInfo.Dispose();
            lightLevels.Dispose();
            moveInfo.Dispose();
            solid.Dispose();
            lootable.Dispose();
        }

        public BlockMovementInfo GetMovementInfo(ushort id)
        {
            return id == 0 ? default : moveInfo[id - 1];
        }

        public bool IsLootable(ushort id)
        {
            if(id == 0) return false;
            return lootable[id - 1];
        }

        public bool IsSolid(ushort id)
        {
            if (id == 0) return false;
            return solid[id - 1];
        }

        public byte GetLightLevel(ushort id)
        {
            if (id == 0) return 0;
            return lightLevels[id - 1];
        }

        public TickInfo GetTickInfo(ushort id)
        {
            if (id == 0) return default;
            return tickInfo[id - 1];
        }
    }

    public struct BlockMovementInfo
    {
        public float movementSpeed;
        public bool walkable;
        public bool door;
    }

    public struct BlockStructureInfo
    {
        public bool structural;
        public bool solid;
        public int roofStrength;
    }

    public enum BlockLevel
    {
        None = 0,
        Floor,
        Wall,
        Roof
    }
}
