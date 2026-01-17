using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using NativeRealm;
using BlockDataRepos;

public static class Extensions
{
    public static Vector2Int ToVector2Int(this Vector3Int input)
    {
        return new Vector2Int(input.x, input.y);
    }

    public static Vector3Int ToVector3Int(this Vector2Int input)
    {
        return new Vector3Int(input.x, input.y, 0);
    }

    public static Vector2 ToVector2(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }
    public static Vector3 ToVector3(this Vector2 vec)
    {
        return new Vector3(vec.x, vec.y, 0);
    }

    public static T SelectRandom<T>(this IEnumerable<T> input)
    {
        if (!input.Any()) return default;
        int rand = UnityEngine.Random.Range(0, input.Count());
        return input.ElementAt(rand);
    }

    public static T SelectRandom<T>(this IEnumerable<T> input, System.Random random)
    {
        if (!input.Any()) return default;
        int rand = random.Next(0, input.Count());
        return input.ElementAt(rand);
    }

    public static TOut SelectRandomWeighted<TOut, TIn>(this IEnumerable<TIn> input, Func<TIn, float> weight, Func<TIn, TOut> value)
    {
        float rand = UnityEngine.Random.Range(0, input.Sum(i => weight(i)));
        float total = 0;
        foreach(var val in input)
        {
            total += weight(val);
            if (rand < total) return value(val);
        }
        return default;
    }

    public static TOut SelectRandomWeighted<TOut, TIn>(this IEnumerable<TIn> input, Func<TIn, float> weight, Func<TIn, TOut> value, System.Random random)
    {
        if (random == null) return SelectRandomWeighted(input, weight, value);
        if (!input.Any()) return default;
        float rand = (float)random.NextDouble() * input.Sum(i => weight(i));
        float total = 0;
        foreach (var val in input)
        {
            total += weight(val);
            if (rand < total) return value(val);
        }
        return default;
    }

    public static string SplitCamelCase(this string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
    }

    public static Dictionary<string, string> ReadStats(this object obj)
    {
        var all = obj.GetType().GetFields()
        .Where(_ => _.GetCustomAttributes(typeof(Stat), true).Length >= 1);

        var attrib = all.ToDictionary(f => f.GetCustomAttributes(typeof(Stat), true).Cast<Stat>().First().Name, 
            f =>
            {
                var val = f.GetValue(obj);
                if (val is null) return null;
                if (float.TryParse(Convert.ToString(val), out var floatVal))
                {
                    var stat = f.GetCustomAttributes(typeof(Stat), true).Cast<Stat>().First();
                    if (floatVal == stat.defaultValue) return null;
                    if (floatVal == -1) return stat.ValueOverride;
                }
                return val.ToString();
            });

        return attrib;
    }

    public static T MinBy<T, TCompare>(this IEnumerable<T> enumerable, Func<T, TCompare> comp) where TCompare : IComparable<TCompare> 
    {
        if (enumerable.Count() <= 0) return default;
        T min = enumerable.First();
        foreach(T t in enumerable.Skip(1))
        {
            if(comp(t).CompareTo(comp(min)) < 0)
            {
                min = t;
            }
        }
        return min;
    }

    public static T MaxBy<T, TCompare>(this IEnumerable<T> enumerable, Func<T, TCompare> comp) where TCompare : IComparable<TCompare>
    {
        if (!enumerable.Any()) return default;
        T max = enumerable.First();
        foreach (T t in enumerable.Skip(1))
        {
            if (comp(t).CompareTo(comp(max)) > 0)
            {
                max = t;
            }
        }
        return max;
    }

    public static Vector2 Abs(this Vector2 vector)
    {
        for (int i = 0; i < 2; ++i) vector[i] = Mathf.Abs(vector[i]);
        return vector;
    }

    public static Vector2 DividedBy(this Vector2 vector, Vector2 divisor)
    {
        for (int i = 0; i < 2; ++i) vector[i] /= divisor[i];
        return vector;
    }

    public static Vector2 Max(this Rect rect)
    {
        return new Vector2(rect.xMax, rect.yMax);
    }

    public static Vector2 IntersectionWithRayFromCenter(this Rect rect, Vector2 pointOnRay)
    {
        Vector2 pointOnRay_local = pointOnRay - rect.center;
        Vector2 edgeToRayRatios = (rect.max - rect.center).DividedBy(pointOnRay_local.Abs());
        return (edgeToRayRatios.x < edgeToRayRatios.y) ?
            new Vector2(pointOnRay_local.x > 0 ? rect.xMax : rect.xMin,
                pointOnRay_local.y * edgeToRayRatios.x + rect.center.y) :
            new Vector2(pointOnRay_local.x * edgeToRayRatios.y + rect.center.x,
                pointOnRay_local.y > 0 ? rect.yMax : rect.yMin);
    }

    public static void RemoveChildren(this Transform obj)
    {
        for (int i = 0; i < obj.childCount; i++)
        {
            GameObject.Destroy(obj.GetChild(i).gameObject);
        }
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, System.Random rand)
    {
        return source.OrderBy((item) => rand.Next());
    }

    public static int ManhattanDistance(this Vector2Int a, Vector2Int b)
    {
        checked
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }

    public static MoveInfo GetMovementInfo(this NativeBlockSlice slice, BlockData groundData, BlockData wallData)
    {
        var speed = slice.isWater && slice.groundBlock == 0 ? 0.5f : (1 + groundData.movementSpeed + wallData.movementSpeed);
        return new()
        {
            movementSpeed = speed,
            walkable = slice.wallBlock == 0 || wallData.walkable,
        };
    }

    public static MoveInfo GetMovementInfo(this NativeBlockSlice slice, NativeBlockDataRepo dataRepo)
    {
        return slice.GetMovementInfo(dataRepo.GetBlock(slice.groundBlock), dataRepo.GetBlock(slice.wallBlock));
    }

    public static MoveInfo GetMovementInfo(this NativeBlockSlice slice)
    {
        return slice.GetMovementInfo(BlockDataRepo.NativeRepo);
    }

    public static byte ToOffsetStateEncoding(this Vector2Int offset)
    {
        var offsetX = offset.x + 8;
        var offsetY = offset.y + 8;
        var res = (byte)(((0b1111 & offsetX) << 4) | (0b1111 & offsetY));
        return res;
    }

    public static Vector2Int ToOffsetState(this byte state)
    {
        var x = ((0b11110000 & state) >> 4) - 8;
        var y = (0b1111 & state) - 8;
        var res = new Vector2Int(x, y);
        return res;
    }

    public static Vector2Int GetProxyOffset(this NativeBlockSlice slice, BlockData wallData)
    {
        if (!wallData.isProxy) return Vector2Int.zero;
        return slice.simpleBlockState.ToOffsetState();
    }

    public static Vector2Int GetProxyOffset(this NativeBlockSlice slice, NativeBlockDataRepo dataRepo)
    {
        return slice.GetProxyOffset(dataRepo.GetBlock(slice.wallBlock));
    }

    public static Vector2Int GetProxyOffset(this NativeBlockSlice slice)
    {
        return slice.GetProxyOffset(BlockDataRepo.NativeRepo);
    }
}

public struct MoveInfo
{
    public bool walkable;
    public float movementSpeed;
}
