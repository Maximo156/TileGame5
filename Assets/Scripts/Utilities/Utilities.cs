using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Random = UnityEngine.Random;
using NativeRealm;
using BlockDataRepos;
using Unity.Mathematics;

public static class Utilities
{
    public static float SampleNormal(float mean, float std)
    {
        double tmp1 = 1 - UnityEngine.Random.Range(0, 1);
        double tmp2 = 1 - UnityEngine.Random.Range(0, 1);

        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(tmp1)) *
                                    Math.Sin(2.0 * Math.PI * tmp2);

        return (float)(mean + std * randStdNormal);
    }

    public static float SampleNormal(float mean, float std, System.Random rand)
    {
        double tmp1 = 1 - rand.NextDouble();
        double tmp2 = 1 - rand.NextDouble();

        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(tmp1)) *
                                    Math.Sin(2.0 * Math.PI * tmp2);

        return (float)(mean + std * randStdNormal);
    }

    public static Vector2Int GetChunk(Vector2Int block, int ChunkWidth)
    {
        return Vector2Int.FloorToInt(new Vector2(block.x, block.y) / ChunkWidth);
    }

    public static Vector2Int GetBlockPos(Vector2 pos)
    {
        return Vector2Int.FloorToInt(pos);
    }

    public static Vector2 GetBlockCenter(Vector2Int pos)
    {
        return pos + 0.5f * Vector2.one;
    }

    public static Vector2Int RandomVector2Int(int range)
    {
        return new Vector2Int(Random.Range(-range, range), Random.Range(-range, range));
    }

    public static Vector2Int RandomVector2Int(int range, System.Random rand)
    {
        return new Vector2Int(rand.Next(-range, range), rand.Next(-range, range));
    }

    public static Vector2Int RandomVector2Int(int min, int max)
    {
        return new Vector2Int(Random.Range(min, max), Random.Range(min, max));
    }

    public static Vector2Int RandomVector2Int(int min, int max, System.Random rand)
    {
        return new Vector2Int(rand.Next(min, max), rand.Next(min, max));
    }

    public static long CurSeconds()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
    }

    public static long CurMilli()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private static BoundsInt CalcChunks(Vector2Int oldChunk, Vector2Int newChunk, int radius)
    {
        var width = 2 * radius + 1;
        var dif = newChunk - oldChunk;
        var pos = (newChunk + (dif * radius)).ToVector3Int();
        var size = new Vector3Int(dif.y == 0 ? 1 : width, dif.x == 0 ? 1 : width, 1);
        var bounds = new BoundsInt(pos - Vector3Int.FloorToInt(size / 2), size);
        return bounds;
    }
    public static void DoForNewChunks(Vector2Int oldChunk, Vector2Int newChunk, int radius, Action<Vector2Int> toDo)
    {

        foreach (var chunk in CalcChunks(oldChunk, newChunk, radius).allPositionsWithin)
        {
            toDo(chunk.ToVector2Int());
        }
    }

    public static IEnumerator EnumerateForNewChunks(Vector2Int oldChunk, Vector2Int newChunk, int radius, Func<Vector2Int, IEnumerator> toDo)
    {
        foreach (var chunk in CalcChunks(oldChunk, newChunk, radius).allPositionsWithin)
        {
            yield return toDo(chunk.ToVector2Int());
        }
    }

    public static IEnumerable<Vector2Int> Spiral(Vector2Int start, uint dist)
    {
        yield return start;

        var offset = new Vector2Int();
        uint layer = 1;
        uint leg = 0;

        while (layer < dist + 1)
        {
            switch (leg)
            {
                case 0: ++offset.x; if (offset.x >= layer) ++leg; break;
                case 1: ++offset.y; if (offset.y >= layer) ++leg; break;
                case 2: --offset.x; if (-offset.x >= layer) ++leg; break;
                case 3: --offset.y; if (-offset.y >= layer) { leg = 0; ++layer; } break;
            }
            yield return start + offset;
        }
    }

    public static Color ColorFromInt(int input)
    {
        var rand = new System.Random(input * 12123);
        return new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
    }

    public static int modulo(int a, int b)
    {
        int r = a % b;
        return r < 0 ? r + b : r;
    }

    public static Dictionary<Item, int> ConvertToItemCounts(IEnumerable<ItemStack> items)
    {
        return items.Where(s => s != null).GroupBy(s => s.Item).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
    }

    public static (Vector2Int position, T result)? BFS<T>(Vector2Int start, Func<Vector2Int, T> getObject, Func<T, bool> isTarget, Func<T, bool> isInvalid, out HashSet<Vector2Int> seen, int limit = 100)
    {
        seen = new HashSet<Vector2Int>();
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();

        toCheck.Enqueue(start);
        int count = 0;
        while(toCheck.Count > 0 && count++ < limit)
        {
            var current = toCheck.Dequeue();
            var obj = getObject(current);
            if (isTarget(obj))
            {
                return (current, obj);
            }
            if (isInvalid(obj))
            {
                continue;
            }
            seen.Add(current);
            foreach(var v in QuadAdjacent)
            {
                var pos = v + current;
                if (!seen.Contains(pos))
                {
                    toCheck.Enqueue(pos);
                }
            }
        }
        return null;
    }

    public static Vector2Int? FindNearestEmptyBlock(Vector2Int start, int limit = 100)
    {
        var width = WorldSettings.ChunkWidth;
        var curChunkPos = GetChunk(start, width);
        ChunkManager.TryGetChunk(curChunkPos, out var curChunk);
        return BFS(start, pos =>
        {
            var tmp = GetChunk(pos, width);
            if (curChunkPos != tmp)
            {
                curChunkPos = tmp;
                ChunkManager.TryGetChunk(curChunkPos, out curChunk);
            }
            return curChunk?.GetBlock(pos);
        }, slice => slice?.wallBlock == 0, _ => false, out var _, limit)?.position;
    }

    public static HashSet<Vector2Int> FindReachableBlocks(Vector2Int start, int reach)
    {
        var width = WorldSettings.ChunkWidth;
        var curChunkPos = GetChunk(start, width);
        ChunkManager.TryGetChunk(curChunkPos, out var curChunk);
        BFS(start, pos => pos, _ => false, pos => Vector2Int.Distance(pos, start) > reach || GetBlock(pos)?.GetMovementInfo().walkable != true, out var reachable, 10000);
        
        Debug.Log($"Count: {reachable.Count} Max dist: {Vector2Int.Distance(start, reachable.MaxBy(v => Vector2Int.Distance(v, start)))}");
        return reachable;

        NativeBlockSlice? GetBlock(Vector2Int pos)
        {
            var tmp = GetChunk(pos, width);
            if (curChunkPos != tmp)
            {
                curChunkPos = tmp;
                ChunkManager.TryGetChunk(curChunkPos, out curChunk);
            }
            return curChunk?.GetBlock(pos);
        }
    }

    public static void DropItems(Vector2Int worldPos, IEnumerable<ItemStack> drops)
    {
        foreach (var stack in drops)
        {
            ItemEntityManager.SpawnItem(worldPos, new ItemStack(stack), true);
        }
    }

    public static List<Type> GetAllConcreteSubclassesOf<T>()
    {
        Type baseType = typeof(T);

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                // Try-catch protects against ReflectionTypeLoadException
                try { return assembly.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(type =>
                baseType.IsAssignableFrom(type) &&
                type.IsClass &&
                !type.IsAbstract)
            .ToList();
    }

    public static Block GetActionableBlock(bool roof, NativeBlockSlice slice)
    {
        return roof ? BlockDataRepo.GetBlock<Block>(slice.roofBlock): (BlockDataRepo.GetBlock<Block>(slice.wallBlock) ?? BlockDataRepo.GetBlock<Block>(slice.groundBlock));
    }

    public static bool CheckChunkBoundry(int x, int y, int ChunkWidth)
    {
        return x >= 0 && y >= 0 && x < ChunkWidth && y < ChunkWidth;
    }

    public static Vector2Int[] QuadAdjacent =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public static int2[] QuadAdjacentInt =
    {
        Vector2Int.up.ToInt(),
        Vector2Int.right.ToInt(),
        Vector2Int.down.ToInt(),
        Vector2Int.left.ToInt()
    };

    public static int2[] OctAdjacentInt =
    {
        Vector2Int.up.ToInt(),
        (Vector2Int.up + Vector2Int.right).ToInt(),
        (Vector2Int.up + Vector2Int.left).ToInt(),
        Vector2Int.right.ToInt(),
        Vector2Int.down.ToInt(),
        (Vector2Int.down + Vector2Int.right).ToInt(),
        (Vector2Int.down + Vector2Int.left).ToInt(),
        Vector2Int.left.ToInt()
    };

    public static Vector2Int[] OctAdjacent =
    {
        Vector2Int.up,
        Vector2Int.up + Vector2Int.right,
        Vector2Int.up + Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.down + Vector2Int.right,
        Vector2Int.down + Vector2Int.left,
        Vector2Int.left
    };

    public static Vector3Int[] QuadAdjacent3 =
    {
        Vector3Int.up,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.down
    };

    public static Vector3Int[] OctAdjacent3 = {
        Vector3Int.up + Vector3Int.left,
        Vector3Int.up,
        Vector3Int.up + Vector3Int.right,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.down + Vector3Int.left,
        Vector3Int.down,
        Vector3Int.down + Vector3Int.right,
    };
}
