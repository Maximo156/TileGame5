using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class CallbackManager : MonoBehaviour
{
    public static CallbackManager manager;
    public int CallbacksPerFrame;
    readonly ConcurrentQueue<DelayedCallback> callbacks = new ConcurrentQueue<DelayedCallback>();
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
                if(action.delay == 0)
                {
                    action.action?.Invoke();
                }
                else
                {
                    action.delay--;
                    callbacks.Enqueue(action);
                }
            }
        }
    }

    public static void AddCallback(Action action, uint delay = 0)
    {
        manager.callbacks.Enqueue(new()
        {
            action = action,
            delay = delay
        });
    }

    struct DelayedCallback
    {
        public uint delay;
        public Action action;
    }
}


