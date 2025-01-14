using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using UnityEngine;
using System;
using System.Linq;

public class Timer
{
    Stopwatch stopwatch;
    int printCount = 0;
    string tag;
    int printCuttoff;
    public Timer(int printCuttoff, string tag = null)
    {
        stopwatch = new Stopwatch();
        stopwatch.Start();
        this.tag = tag ?? "Timer";
        this.printCuttoff = printCuttoff;
    }

    public void print()
    {
        if (stopwatch.ElapsedMilliseconds > printCuttoff)
        {
            UnityEngine.Debug.Log($"{tag} {printCount}: {stopwatch.ElapsedMilliseconds}");
        }
        printCount++;
    }

    public void printAndReset()
    {
        if(openIntervals.Count != 0)
        {
            throw new InvalidOperationException("Interval recording interupted");
        }
        print();
        stopwatch.Restart();
    }

    public void printIntervals()
    {
        foreach(var kvp in intervalsSums)
        {
            UnityEngine.Debug.Log($"{tag} {kvp.Key}: {kvp.Value}");
        }
    }

    public long GetInterval(string key)
    {
        return intervalsSums[key];
    }

    public void printInterval(string key)
    {
        UnityEngine.Debug.Log($"{intervalsSums[key]}");
    }

    Dictionary<string, long> openIntervals = new();
    Dictionary<string, long> intervalsSums = new();
    public void StartInterval(string key)
    {
        openIntervals.Add(key, stopwatch.ElapsedMilliseconds);
    }

    public void StopInterval(string key)
    {
        intervalsSums.TryGetValue(key, out long existing);
        if(openIntervals.Remove(key, out var start))
        {
            intervalsSums[key] = (stopwatch.ElapsedMilliseconds - start) + existing;
        }
        else
        {
            throw new InvalidOperationException("Interval has not started");
        }
        var AvgKey = $"{tag}: {key}";
        if(average.TryGetValue(AvgKey, out var info))
        {
            var newAvg = info.avg + ((intervalsSums[key] - info.avg) / (info.count + 1));
            average[AvgKey] = (info.count + 1, newAvg);
        }
        else
        {
            average[AvgKey] = (1, intervalsSums[key]);
        }
    }

    static ConcurrentDictionary<string, (int count, long avg)> average = new();

    public static void PrintAverages()
    {
        foreach (var kvp in average.ToList())
        {
            UnityEngine.Debug.Log($"{kvp.Key}: {kvp.Value.avg}");
        }
    }
}
