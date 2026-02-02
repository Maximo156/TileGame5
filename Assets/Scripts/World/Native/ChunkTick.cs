using BlockDataRepos;
using NativeRealm;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
public static class ChunkTick
{
    public static ChunkTickJobInfo ScheduelTick(Vector2Int pos, RealmData data)
    {
        var tickDistance = WorldSettings.ChunkTickDistance;
        var chunks = new NativeArray<int2>((tickDistance*2+1) * (tickDistance * 2 + 1), Allocator.TempJob);
        int c = 0;
        for (int x = -tickDistance + pos.x; x <= tickDistance + pos.x; x++)
        {
            for (int y = -tickDistance + pos.y; y <= tickDistance + pos.y; y++)
            {
                chunks[c++] = math.int2(x, y);
            }
        }
        var updates = new NativeQueue<TickBlockUpdate>(Allocator.Persistent);

        var jobHandle = new ChunkTickJob()
        {
            chunkWidth = WorldSettings.ChunkWidth,
            worldTickMs = WorldSettings.TickMs,
            chunks = chunks,
            realmData = data,
            blockDataRepo = BlockDataRepo.NativeRepo,
            updates = updates.AsParallelWriter(),
            randomSeed = (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)
        }.Schedule(chunks.Length, 1);

        var cleanup = chunks.Dispose(jobHandle);

        return new()
        {
            job = cleanup,
            updates = updates,
            schedueled = Time.time,
            needsProcessing = true
        };
    }

    public static JobHandle WriteUpdates(ChunkTickJobInfo info, RealmData data)
    {
        var updateArray = info.updates.ToArray(Allocator.TempJob);
        var jobHandle = new UpdateWriteJob()
        {
            array = updateArray,
            realmData = data
        }.Schedule(info.job);
        return updateArray.Dispose(jobHandle);
    }

    [BurstCompile]
    partial struct ChunkTickJob : IJobParallelFor
    {
        public int chunkWidth;
        public int worldTickMs;
        public uint randomSeed;
        [ReadOnly]
        public NativeArray<int2> chunks;

        [ReadOnly]
        public RealmData realmData;

        [ReadOnly]
        public NativeBlockDataRepo blockDataRepo;

        [WriteOnly]
        public NativeQueue<TickBlockUpdate>.ParallelWriter updates;

        public void Execute(int index)
        {
            var rand = new Random(randomSeed);
            var chunkPos = chunks[index];
            if (!realmData.TryGetChunk(chunkPos, out var chunk)) return;

            for(int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    if(blockDataRepo.TryGetBlock(chunk.GetWall(x, y), out var wall))
                    {
                        ProcessBlock(chunkPos, math.int2(x, y), wall, ref rand);
                    }
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessBlock(int2 chunk, int2 local, BlockData block, ref Random rand)
        {
            switch (block.tickBehaviour)
            {
                case TickBehaviour.Replace: 
                    Replace(chunk, local, block.replaceBehaviour, ref rand);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Replace(int2 chunk, int2 local, ReplaceBehaviourInfo info, ref Random rand)
        {
            if (rand.NextDouble() < 1 - math.pow(math.E, -1f * worldTickMs / (1000 * info.MeanSecondsToHappen)))
            {
                updates.Enqueue(new() { chunk = chunk, localPos = local, newBlock = info.nextBlock });
            }
        }
    }

    [BurstCompile]
    partial struct UpdateWriteJob : IJob
    {
        public NativeArray<TickBlockUpdate> array;

        public RealmData realmData;

        public void Execute()
        {
            for(int i = 0; i< array.Length; i++)
            {
                var update = array[i];
                if(realmData.TryGetChunk(update.chunk, out var chunk))
                {
                    chunk.SetWall(update.localPos.x, update.localPos.y, update.newBlock);
                }
            }
        }
    }

    public struct TickBlockUpdate
    {
        public int2 chunk;
        public int2 localPos;
        public ushort newBlock;
    }
}

public struct ChunkTickJobInfo
{
    public float schedueled;
    public JobHandle job;
    public NativeQueue<ChunkTick.TickBlockUpdate> updates;
    public bool needsProcessing;

    public void Dispose()
    {
        updates.Dispose();
        needsProcessing = false;
    }
}
