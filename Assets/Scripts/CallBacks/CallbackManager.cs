using System;
using System.Collections.Concurrent;
using UnityEngine;

public class CallbackManager : MonoBehaviour
{
    public static CallbackManager manager;
    public int CallbacksPerFrame;
    readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
    // Start is called before the first frame update
    void Awake()
    {
        manager = this;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        int count = 0;
        while (callbacks.Count > 0 && count < CallbacksPerFrame)
        {
            count++;
            if (callbacks.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }

    public static void AddCallback(Action action)
    {
        manager.callbacks.Enqueue(action);
    }
}
