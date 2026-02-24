using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using ComposableBlocks;

namespace BlockDataRepos
{
    public class BlockDataRepo : MonoBehaviour
    {
        public static NativeBlockDataRepo NativeRepo;
        public static ProxyBlock proxyBlock;
        static Block[] Blocks;

        private void Awake()
        {
            var tmp = Resources.LoadAll<Block>("ScriptableObjects/Blocks").OrderBy(b => b.name).ToList();
            proxyBlock = tmp.First(b => b is ProxyBlock) as ProxyBlock;

            Debug.Log($"Loading {tmp.Count} blocks");
            Blocks = new Block[tmp.Count];

            ValidateIds(tmp);

            NativeRepo = new(tmp);
            foreach (var block in tmp)
            {
                Blocks[block.Id-1] = block;
            }
        }

        private void ValidateIds(List<Block> blocks)
        {
#if UNITY_EDITOR
            try
            {
                var groups = blocks.GroupBy(b => b.Id);
                bool duplicate = false;
                foreach (var group in groups)
                {
                    if (group.Count() > 1)
                    {
                        duplicate = true;
                        Debug.LogError($"Duplicate id {group.Key}: {string.Join(", ", group)}");
                    }
                }
                if (duplicate)
                {
                    throw new System.Exception("Duplicate Block ID's Detected");
                }

                var set = new HashSet<ushort>(blocks.Select(b => b.Id)); 
                var max = blocks.Max(b => b.Id);
                if (set.Count != max)
                {
                    var missing = Enumerable.Range(1, max).Where(i => !set.Contains((ushort)i));
                    var over = blocks.Where(b => b.Id > set.Count).Select(b => b.name);
                    throw new System.Exception($"Missing block ids: {string.Join(", ", missing)}, potential fillers: {string.Join(", ", over)}");
                }
            } catch
            { 
                EditorApplication.isPlaying = false;
                throw;
            }
#endif
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
            if(id != 0)
            {
                block = Blocks[id - 1] as T;
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
                ProcessBlock(blocks[i]);
            }
        }

        public void ProcessBlock(Block block)
        {
            var index = block.Id - 1;
            lightLevels[index] = block.TryGetBehavior<LightBehaviour>(out var light) ? (byte)(light.LightLevel * 2) : (byte)0;
            var wall = block as Wall;
            moveInfo[index] = new BlockMovementInfo 
            { 
                door = block.TryGetBehavior<DoorBehaviour>(out var _),
                movementSpeed = block.MovementModifier,
                walkable = wall?.walkable ?? true,
            };
            solid[index] = wall?.solid ?? false;
            lootable[index] = block.TryGetBehavior<ILootableBlockBehaviour>(out var _);
            tickInfo[index] = block.TryGetBehavior<ISimpleTickBlockBehaviour>(out var tick) ? tick.GetTickInfo() : default;
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
