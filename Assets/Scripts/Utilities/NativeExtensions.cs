using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public static class NativeExtensions
{
    public static T SelectRandomWeighted<T>(
        this NativeSlice<T> input,
        ref Random random)
        where T : struct, IWeighted
    {
        int length = input.Length;
        if (length == 0)
            return default;

        float totalWeight = 0f;
        for (int i = 0; i < length; i++)
            totalWeight += input[i].Weight;

        float rand = random.NextFloat(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < length; i++)
        {
            cumulative += input[i].Weight;
            if (rand < cumulative)
                return input[i];
        }

        return default;
    }

    public static int SelectRandomIndexWeighted<T>(
        this NativeSlice<T> input,
        ref Random random)
        where T : struct, IWeighted
    {
        int length = input.Length;
        if (length == 0)
            return default;

        float totalWeight = 0f;
        for (int i = 0; i < length; i++)
            totalWeight += input[i].Weight;

        float rand = random.NextFloat(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < length; i++)
        {
            cumulative += input[i].Weight;
            if (rand < cumulative)
                return i;
        }

        return -1;
    }

    public static int SelectRandomIndexWeighted<T>(
        this NativeList<T> input,
        ref Random random)
        where T : unmanaged, IWeighted
    {
        int length = input.Length;
        if (length == 0)
            return default;

        float totalWeight = 0f;
        for (int i = 0; i < length; i++)
            totalWeight += input[i].Weight;

        float rand = random.NextFloat(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < length; i++)
        {
            cumulative += input[i].Weight;
            if (rand < cumulative)
                return i;
        }

        return -1;
    }

    public static void Shuffle<T>(this NativeList<T> source, Random rand) where T : unmanaged
    {
        for (int i = source.Length-1; i >= 0; i--)
        {
            var j = rand.NextInt(i);
            (source[i], source[j]) = (source[j], source[i]);
        }
    }

    public static T SelectRandom<T>(this NativeSlice<T> input, ref Random random) where T : unmanaged
    {
        if (input.Length == 0) return default;
        int rand = random.NextInt(0, input.Length);
        return input[rand];
    }

    public static T GetElement2d<T>(this NativeSlice<T> array, int x, int y, int chunkWidth) where T : struct
    {
        return array[x * chunkWidth + y];
    }

    public static void SetElement2d<T>(this NativeSlice<T> array, int x, int y, int chunkWidth, T item) where T : struct
    {
        array[x * chunkWidth + y] = item;
    }

    public static T GetElement2d<T>(this NativeArray<T> array, int x, int y, int chunkWidth) where T : struct
    {
        return array[x * chunkWidth + y];
    }

    public static void SetElement2d<T>(this NativeArray<T> array, int x, int y, int chunkWidth, T item) where T : struct
    {
        array[x * chunkWidth + y] = item;
    }

    public static NativeSlice<T> GetChunk<T>(this NativeArray<T> array, int index, int chunkLength) where T : struct
    {
        return array.Slice(index * chunkLength, chunkLength);
    }


    public delegate void SetSliceDataAction<TSlice>(ref TSlice item, SliceData data);
    public static void ToNativeSlices<TIn, TOut, TSlice>(
        this IEnumerable<TIn> input, 
        Func<TIn, IEnumerable<TOut>> dataConverter, 
        Func<TIn, TSlice> itemConverter,
        SetSliceDataAction<TSlice> setSliceData,
        out NativeArray<TOut> data, 
        out NativeArray<TSlice> slices) where TOut : unmanaged where TSlice : unmanaged, IHasSliceData
    {
        int count = 0;
        int index = 0;
        var dataList = new NativeList<TOut>(0, Allocator.Persistent);
        slices = new NativeArray<TSlice>(input.Count(), Allocator.Persistent);
        foreach ( var item in input )
        {
            var convertedItem = itemConverter(item);

            var itemData = dataConverter(item);
            var itemStart = count;
            if (itemData != null)
            {
                foreach (var d in itemData)
                {
                    count++;
                    dataList.Add(d);
                }
            }
            setSliceData(ref convertedItem, new()
            {
                start = itemStart,
                length = count - itemStart
            });
            slices[index] = convertedItem;

            index++;
        }
        data = new NativeArray<TOut>(dataList, Allocator.Persistent);
        dataList.Dispose();
    }
}

public struct SliceData
{
    public int start;
    public int length;
}

public struct MoveInfo
{
    public bool walkable;
    public float movementSpeed;
}

public interface IWeighted
{
    int Weight { get; }
}

public interface IHasSliceData
{
    SliceData sliceData { get; set; }
}
