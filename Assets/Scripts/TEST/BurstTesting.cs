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
        var position = new NativeArray<Vector2Int>(500, Allocator.Temp);
        var output = new NativeArray<float>(1, Allocator.Persistent);
        for (int i = 0; i < position.Length; i++)
            position[i] = Utilities.RandomVector2Int(100);
        /*
        var job = new TestJob
        {
            Input = ChunkManager.CurRealm.chunkDataMirror,
            Output = output
        };
        job.Schedule().Complete();*/

        Debug.Log("The result of the sum is: " + output[0]);
        //input.Dispose();
        output.Dispose();
    }

    [BurstCompile]
    private struct TestJob : IJobParallelFor
    {
        [ReadOnly]
        //public NativeHashMap<int2, BlockSliceData> Input;

        public float Range;

        [WriteOnly]
        public NativeArray<float> Output;

        [BurstCompile]
        public void Execute(int i)
        {
            for(int x = 0; x<Range; x++)
            {
                for (int y = 0; y < Range; y++)
                {
                    //Output[x + y] = Input[new int2(x, y)].MovementSpeed;
                }
            }
        }
    }
}
