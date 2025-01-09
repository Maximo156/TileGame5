using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System;

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
    }
}
