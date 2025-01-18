using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public class BurstTesting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var input = new NativeArray<float>(10, Allocator.Persistent);
        var output = new NativeArray<float>(1, Allocator.Persistent);
        for (int i = 0; i < input.Length; i++)
            input[i] = 1.0f * i;

        var job = new TestJob
        {
            Input = new float2(0, 0),
            Output = output
        };
        job.Schedule().Complete();

        Debug.Log("The result of the sum is: " + output[0]);
        input.Dispose();
        output.Dispose();
    }

    [BurstCompile]
    private struct TestJob : IJob
    {
        [ReadOnly]
        public float2 Input;

        [ReadOnly]
        public float Range;

        [WriteOnly]
        public NativeArray<float> Output;

        public void Execute()
        {
            for(int x = 0; x<Range; x++)
            {
                for (int y = 0; y < Range; y++)
                {
                    //ChunkManager.GetMovementSpeed()
                }
            }
        }
    }

}
