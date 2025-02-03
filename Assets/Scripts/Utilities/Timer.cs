using UnityEngine;


public class Timer
{
    float end;
    public bool Expired => Time.time > end;
    public float SecondsLeft => end - Time.time;
    public Timer(float duration)
    {
        end = Time.time + duration;
    }
}

