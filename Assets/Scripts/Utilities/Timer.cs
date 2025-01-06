using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

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
        print();
        stopwatch.Restart();
    }
}
