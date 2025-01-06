using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[CreateAssetMenu(fileName = "NewStructureGenerator", menuName = "Terrain/StructureGenerator", order = 1)]
public class StructureGenerator : ChunkSubGenerator
{
    public int seed;
    public int StructureNoiseChunkSize;
    public List<Structure> Structures;

    ConcurrentDictionary<Vector2Int, Task<Dictionary<Vector2Int, BlockSlice[,]>>> genTasks = new();
    ConcurrentDictionary<Vector2Int, (Vector2Int point, System.Random rand)> Chunks = new();
    //ConcurrentDictionary<Vector2Int, BlockSlice[,]> Loaded = new();
    public override async Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand)
    {
        var inStructChunk = Vector2Int.FloorToInt(new Vector2(WorldPosition.x, WorldPosition.y) / StructureNoiseChunkSize);
        Vector2Int closestStructChunk = Vector2Int.one;
        (Vector2Int point, System.Random rand) closest = default;
        var dist = float.MaxValue;
        foreach (var v in Utilities.OctAdjacent.Select(v => v + inStructChunk).Append(inStructChunk))
        {
            if (!Chunks.TryGetValue(v, out var info))
            {
                info = GenPoint(v);
                Chunks.TryAdd(v, info);
            }
            if (Vector2Int.Distance(WorldPosition, info.point) < dist)
            {
                closest = info;
                closestStructChunk = v;
                dist = Vector2Int.Distance(WorldPosition, info.point);
            }
        }
        Task<Dictionary<Vector2Int, BlockSlice[,]>> task;
        lock (genTasks) {
            if (!genTasks.TryGetValue(closestStructChunk, out task))
            {
                task = GenerateStructurePoint(closest, closestStructChunk, biomeInfo, blocks.GetLength(0));
                genTasks.TryAdd(closestStructChunk, task);
            }
        }
        var res = await task;
        if (res.Remove(ChunkPosition, out var structBlocks))
        {
            Overlay(ref blocks, structBlocks);
        }
        
        /*
        if (!Loaded.ContainsKey(ChunkPosition))
        {
            var structChunk = Vector2Int.FloorToInt(new Vector2(WorldPosition.x, WorldPosition.y) / StructureNoiseChunkSize);
            Vector2Int closestStructChunk = Vector2Int.one;
            (Vector2Int point, bool gened, System.Random rand) closest = default;
            var dist = float.MaxValue;
            foreach (var v in Utilities.OctAdjacent.Select(v => v + structChunk).Append(structChunk))
            {
                if(!Chunks.TryGetValue(v, out var info))
                {
                    info = GenPoint(v);
                    Chunks.TryAdd(v, info);
                }
                if (Vector2Int.Distance(WorldPosition, info.point) < dist)
                {
                    closest = info;
                    closestStructChunk = v;
                }
            }
            if (closest.gened)
            {
                return;
            }

            var surroundingClosest = new List<Vector2Int>();
            foreach (var v in Utilities.OctAdjacent.Select(v => v + closestStructChunk))
            {
                if (!Chunks.TryGetValue(v, out var surrounding))
                {
                    surrounding = GenPoint(v);
                    Chunks.TryAdd(v, surrounding);
                }
                surroundingClosest.Add(surrounding.point);
            }
            closest.gened = true;
            Chunks[closestStructChunk] = closest;
            var structure = Structures.SelectRandom(closest.rand);
            if (structure != null)
            {
                foreach (var kvp in structure.Generate(closest.point, biomeInfo, blocks.GetLength(0), closest.rand, surroundingClosest))
                {
                    Loaded.TryAdd(kvp.Key, kvp.Value);
                }
            }
        }
        if(Loaded.Remove(ChunkPosition, out var structBlocks))
        {
            Overlay(ref blocks, structBlocks);
        }*/
    }

    Task<Dictionary<Vector2Int, BlockSlice[,]>> GenerateStructurePoint((Vector2Int point, System.Random rand) closest, Vector2Int closestStructChunk, BiomeInfo biomeInfo, int chunkWidth)
    {
        return Task.Run(() =>
        {
            var surroundingClosest = new List<Vector2Int>();
            foreach (var v in Utilities.OctAdjacent.Select(v => v + closestStructChunk))
            {
                if (!Chunks.TryGetValue(v, out var surrounding))
                {
                    surrounding = GenPoint(v);
                    Chunks.TryAdd(v, surrounding);
                }
                surroundingClosest.Add(surrounding.point);
            }
            var structure = Structures.SelectRandom(closest.rand);
            if (structure != null)
            {
                return structure.Generate(closest.point, biomeInfo, chunkWidth, closest.rand, surroundingClosest);
            }
            return null;
        });
    }

    void Overlay(ref BlockSlice[,] blocks, BlockSlice[,] structure)
    {
        for(int x = 0; x < blocks.GetLength(0); x++)
        {
            for (int y = 0; y < blocks.GetLength(0); y++)
            {
                if (structure[x, y]?.HasBlock() == true)
                {
                    var StructureBlock = structure[x, y];
                    var initialBlock = blocks[x, y];
                    StructureBlock.Water = initialBlock.Water;
                    StructureBlock.MovementSpeed = initialBlock.MovementSpeed;
                    blocks[x, y] = StructureBlock;
                }
            }
        }
    }
    Vector2Int debugChunk = new Vector2Int(0, -1);
    (Vector2Int point, System.Random rand) GenPoint(Vector2Int StructureChunk)
    {
        var seed = StructureChunk.GetHashCode();
        var rand = new System.Random(seed);
        var pos = (StructureChunk * StructureNoiseChunkSize) + new Vector2Int(rand.Next(0, StructureNoiseChunkSize), rand.Next(0, StructureNoiseChunkSize));
        return (pos, rand);
    }

    public void DebugDraw()
    {
        foreach(var kvp in Chunks)
        {
            var chunkPos = kvp.Key * StructureNoiseChunkSize;
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube((chunkPos + Vector2.one * 0.5f * StructureNoiseChunkSize).ToVector3(), Vector2.one * StructureNoiseChunkSize);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(kvp.Value.point.ToVector3Int(), 1);
        }
    }
}
