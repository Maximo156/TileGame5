using BlockDataRepos;
using NativeRealm;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public static class LightCalculation
{
    static bool debug = false;
    public static NativeArray<byte> PreviousBorder;
    public static NativeArray<int2> PreviousChunks;

    public static LightJobInfo ScheduelLightUpdate(RealmData realmData, NativeList<int2> frameUpdatedChunks)
    {
        var chunkWidth = WorldSettings.ChunkWidth;
        var hashChunks = new HashSet<int2>();
        foreach (var chunk in frameUpdatedChunks)
        {
            hashChunks.Add(chunk);
            foreach (var v in Utilities.OctAdjacentInt)
            {
                hashChunks.Add(v + chunk);
            }
        }

        var lightUpdatedChunks = new NativeArray<int2>(hashChunks.ToArray(), Allocator.TempJob);
        var light = new NativeArray<byte>(lightUpdatedChunks.Length * chunkWidth * chunkWidth, Allocator.TempJob);

        if (lightUpdatedChunks.Length == 0)
        {
            return new()
            {
                light = light,
                jobHandle = default,
                updatedChunks = lightUpdatedChunks,
            };
        }

        InitDebug(lightUpdatedChunks.Length, debug);

        var borderBufferRead = new NativeArray<byte>(lightUpdatedChunks.Length * chunkWidth * 4, Allocator.TempJob);
        var borderBufferWrite = new NativeArray<byte>(lightUpdatedChunks.Length * chunkWidth * 4, Allocator.TempJob);

        var intraChunkLight = new IntraChunkLightPropogationJob()
        {
            chunkWidth = chunkWidth,
            chunks = lightUpdatedChunks,
            blockDataRepo = BlockDataRepo.NativeRepo,
            realmData = realmData,
            Light = light,
            borderLightWrite = borderBufferRead,
        }.Schedule(lightUpdatedChunks.Length, 1); 

        var lightingPasses = Mathf.CeilToInt((GameSettings.MaxLightLevel - 1) / 3f);

        JobHandle interChunkHandleDependency = intraChunkLight;
        for(int i = 0; i < lightingPasses; i++)
        {
            interChunkHandleDependency = new InterChunkLightPropogationJob()
            {
                chunkWidth = chunkWidth,
                chunks = lightUpdatedChunks,
                blockDataRepo = BlockDataRepo.NativeRepo,
                realmData = realmData,
                Light = light,
                borderLightRead = borderBufferRead,
                borderLightWrite = borderBufferWrite,
            }.Schedule(lightUpdatedChunks.Length, 1, interChunkHandleDependency);

            (borderBufferWrite, borderBufferRead) = (borderBufferRead, borderBufferWrite);
        }

        JobHandle finalJob = CopyForDebug(lightUpdatedChunks, borderBufferRead, interChunkHandleDependency, debug);

        var cleanup = JobHandle.CombineDependencies(borderBufferRead.Dispose(finalJob), borderBufferWrite.Dispose(finalJob));

        return new()
        {
            light = light,
            jobHandle = cleanup,
            updatedChunks = lightUpdatedChunks,
        };
    }

    public static JobHandle CopyLight(RealmData realmData, LightJobInfo jobInfo, NativeQueue<LightUpdateInfo> updates)
    {
        return new CopyLightJob()
        {
            chunkWidth = WorldSettings.ChunkWidth, 
            chunks = jobInfo.updatedChunks,
            realmData = realmData,
            light = jobInfo.light,
            lightUpdates = updates.AsParallelWriter(),
        }.Schedule(jobInfo.updatedChunks.Length, 1, jobInfo.jobHandle);
    }

    static void InitDebug(int length, bool debug)
    {
        if (!debug) return;

        int chunkWidth = WorldSettings.ChunkWidth;
        if (PreviousBorder.IsCreated)
        {
            PreviousBorder.Dispose();
        }
        if (PreviousChunks.IsCreated)
        {
            PreviousChunks.Dispose();
        }

        PreviousChunks = new NativeArray<int2>(length, Allocator.Persistent);
        PreviousBorder = new NativeArray<byte>(length * chunkWidth * 4, Allocator.Persistent);
    }

    static JobHandle CopyForDebug(NativeArray<int2> chunks, NativeArray<byte> buffer, JobHandle dep, bool debug)
    {
        if (!debug)
        {
            return dep;
        }

        var copy1 = new ArrayCopyJob<byte>()
        {
            src = buffer,
            dest = PreviousBorder,
        }.Schedule(dep);

        var copy2 = new ArrayCopyJob<int2>()
        {
            src = chunks,
            dest = PreviousChunks,
        }.Schedule(dep);

        return JobHandle.CombineDependencies(copy1, copy2);
    }


    [BurstCompile]
    partial struct IntraChunkLightPropogationJob : IJobParallelFor
    {
        public int chunkWidth;
        [ReadOnly]
        public NativeArray<int2> chunks;

        [NativeDisableParallelForRestriction]
        public NativeArray<byte> Light;

        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<byte> borderLightWrite;

        [ReadOnly]
        public RealmData realmData;
        [ReadOnly]
        public NativeBlockDataRepo blockDataRepo;

        public void Execute(int index)
        {
            var chunkWidth = this.chunkWidth;
            var blockDataRepo = this.blockDataRepo;
            var borderLightWrite = this.borderLightWrite;

            var chunkPosition = chunks[index];
            if (!realmData.TryGetChunk(chunkPosition, out var chunk)) return;
            var lightSlice = Light.GetChunk(index, chunkWidth * chunkWidth);
            var updateQueue = new NativeQueue<int2>(Allocator.Temp);

            for (int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    if (blockDataRepo.TryGetBlock(chunk.GetWall(x, y), out var blockData) && blockData.lightLevel != 0)
                    {
                        lightSlice.SetElement2d(x, y, chunkWidth, blockData.lightLevel);
                        updateQueue.Enqueue(math.int2(x + 1, y));
                        updateQueue.Enqueue(math.int2(x - 1, y));
                        updateQueue.Enqueue(math.int2(x, y + 1));
                        updateQueue.Enqueue(math.int2(x, y - 1));
                    }
                }
            }
            int safety = 0;
            while (updateQueue.TryDequeue(out var pos) && safety++ < 5000)
            {
                ProcessPoint(pos);
            }

            WriteBorders();
            updateQueue.Dispose();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ProcessPoint(int2 pos)
            {
                int x = pos.x;
                int y = pos.y;
                if (x < 0 || x >= chunkWidth || y < 0 || y >= chunkWidth) return;
                if (!blockDataRepo.TryGetBlock(chunk.GetWall(x, y), out var blockData) || blockData.lightLevel != 0) return;


                var target = CalcTarget(x, y);

                if (lightSlice.GetElement2d(x, y, chunkWidth) != target)
                {
                    lightSlice.SetElement2d(x, y, chunkWidth, (byte)target);
                    updateQueue.Enqueue(math.int2(x + 1, y));
                    updateQueue.Enqueue(math.int2(x - 1, y));
                    updateQueue.Enqueue(math.int2(x, y + 1));
                    updateQueue.Enqueue(math.int2(x, y - 1));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            uint GetAvailableLight(int x, int y)
            {
                if (!Utilities.CheckChunkBoundry(x, y, chunkWidth)) return 0;

                // Inside current chunk
                if (!blockDataRepo.TryGetBlock(chunk.GetWall(x, y), out var blockData)) return 0;
                if (blockData.solid) return 0;
                return blockData.lightLevel > 0 ? blockData.lightLevel : lightSlice.GetElement2d(x, y, chunkWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void WriteBorders()
            {
                int baseOffset = index * (4 * chunkWidth);

                for (int i = 0; i < chunkWidth; i++)
                {
                    // Left
                    borderLightWrite[baseOffset + 0 * chunkWidth + i] = (byte)GetAvailableLight(0, i);

                    // Right
                    borderLightWrite[baseOffset + 1 * chunkWidth + i] = (byte)GetAvailableLight(chunkWidth - 1, i);

                    // Bottom 
                    borderLightWrite[baseOffset + 2 * chunkWidth + i] = (byte)GetAvailableLight(i, 0);

                    // Top
                    borderLightWrite[baseOffset + 3 * chunkWidth + i] = (byte)GetAvailableLight(i, chunkWidth - 1);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            uint CalcTarget(int x, int y)
            {
                var lAvailable = GetAvailableLight(x - 1, y);
                var rAvailable = GetAvailableLight(x + 1, y);
                var uAvailable = GetAvailableLight(x, y + 1);
                var dAvailable = GetAvailableLight(x, y - 1);

                var lrMax = math.max(lAvailable, rAvailable);
                bool lrSame = lAvailable == rAvailable;
                var udMax = math.max(uAvailable, dAvailable);
                bool udSame = uAvailable == dAvailable;

                return (uint)(math.max(0, math.max(lrMax - (lrSame ? 1 : 2), udMax - (udSame ? 1 : 2))));
            }
        }
    }

    [BurstCompile]
    partial struct InterChunkLightPropogationJob : IJobParallelFor
    {
        public int chunkWidth;
        [ReadOnly]
        public NativeArray<int2> chunks;

        [ReadOnly]
        public NativeArray<byte> borderLightRead;

        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<byte> borderLightWrite;

        [NativeDisableParallelForRestriction]
        public NativeArray<byte> Light;

        [ReadOnly]
        public RealmData realmData;
        [ReadOnly]
        public NativeBlockDataRepo blockDataRepo;

        public void Execute(int index)
        {
            var chunkWidth = this.chunkWidth;
            var blockDataRepo = this.blockDataRepo;
            var borderLightRead = this.borderLightRead;
            var borderLightWrite = this.borderLightWrite;

            var chunkPosition = chunks[index];
            if (!realmData.TryGetChunk(chunkPosition, out var chunk)) return;
            var lightSlice = Light.GetChunk(index, chunkWidth * chunkWidth);
            var updateQueue = new NativeQueue<int2>(Allocator.Temp);

            var rIndex = ChunkIndex(chunkPosition + math.int2(1, 0));
            var lIndex = ChunkIndex(chunkPosition + math.int2(-1, 0));
            var uIndex = ChunkIndex(chunkPosition + math.int2(0, 1)); 
            var dIndex = ChunkIndex(chunkPosition + math.int2(0, -1));

            for (int i = 0; i < chunkWidth; i++)
            {
                updateQueue.Enqueue(math.int2(i, 0));
                updateQueue.Enqueue(math.int2(0, i));
                updateQueue.Enqueue(math.int2(i, chunkWidth-1));
                updateQueue.Enqueue(math.int2(chunkWidth-1, i));
            }

            int safety = 0;
            while (updateQueue.TryDequeue(out var pos) && safety++ < 5000)
            {
                ProcessPoint(pos);
            }

            WriteBorders();

            updateQueue.Dispose();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ProcessPoint(int2 pos)
            {
                int x = pos.x;
                int y = pos.y;
                if (x < 0 || x >= chunkWidth || y < 0 || y >= chunkWidth) return;
                if (!blockDataRepo.TryGetBlock(chunk.GetWall(x, y), out var blockData) || blockData.lightLevel != 0) return;


                var target = CalcTarget(x, y);

                if (lightSlice.GetElement2d(x, y, chunkWidth) != target)
                {
                    lightSlice.SetElement2d(x, y, chunkWidth, (byte)target);
                    updateQueue.Enqueue(math.int2(x + 1, y));
                    updateQueue.Enqueue(math.int2(x - 1, y));
                    updateQueue.Enqueue(math.int2(x, y + 1));
                    updateQueue.Enqueue(math.int2(x, y - 1));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            uint CalcTarget(int x, int y)
            {
                var lAvailable = GetAvailableLight(x - 1, y);
                var rAvailable = GetAvailableLight(x + 1, y);
                var uAvailable = GetAvailableLight(x, y + 1);
                var dAvailable = GetAvailableLight(x, y - 1);

                var lrMax = math.max(lAvailable, rAvailable);
                bool lrSame = lAvailable == rAvailable;
                var udMax = math.max(uAvailable, dAvailable);
                bool udSame = uAvailable == dAvailable;

                var suggestedTarget = (uint)(math.max(0, math.max(lrMax - (lrSame ? 1 : 2), udMax - (udSame ? 1 : 2))));
                if ((x == 0 && lIndex == -1) ||
                    (x == chunkWidth-1 && rIndex == -1) ||
                    (y == 0 && dIndex == -1) ||
                    (y == chunkWidth - 1 && uIndex == -1)) return math.max(suggestedTarget, chunk.GetLight(x, y));

                return suggestedTarget;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            uint GetAvailableLight(int x, int y)
            {
                if (x < 0)
                {
                    if (lIndex == -1) return 0;
                    int baseOffset = lIndex * (4 * chunkWidth);
                    return borderLightRead[baseOffset + 1 * chunkWidth + y]; // Right edge of left neighbor
                }

                if (x >= chunkWidth)
                {
                    if (rIndex == -1) return 0;
                    int baseOffset = rIndex * (4 * chunkWidth);
                    return borderLightRead[baseOffset + 0 * chunkWidth + y]; // Left edge of right neighbor
                }

                if (y < 0)
                {
                    if (dIndex == -1) return 0;
                    int baseOffset = dIndex * (4 * chunkWidth);
                    return borderLightRead[baseOffset + 3 * chunkWidth + x]; // Top edge of bottom neighbor
                }

                if (y >= chunkWidth)
                {
                    if (uIndex == -1) return 0;
                    int baseOffset = uIndex * (4 * chunkWidth);
                    return borderLightRead[baseOffset + 2 * chunkWidth + x]; // Bottom edge of top neighbor
                }

                // Inside current chunk
                if (!blockDataRepo.TryGetBlock(chunk.GetWall(x, y), out var blockData)) return 0;
                if (blockData.solid) return 0;
                return blockData.lightLevel > 0 ? blockData.lightLevel : lightSlice.GetElement2d(x, y, chunkWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void WriteBorders()
            {
                int baseOffset = index * (4 * chunkWidth);

                for (int i = 0; i < chunkWidth; i++)
                {
                    // Left
                    borderLightWrite[baseOffset + 0 * chunkWidth + i] = (byte)GetAvailableLight(0, i);

                    // Right
                    borderLightWrite[baseOffset + 1 * chunkWidth + i] = (byte)GetAvailableLight(chunkWidth - 1, i);

                    // Bottom 
                    borderLightWrite[baseOffset + 2 * chunkWidth + i] = (byte)GetAvailableLight(i, 0);

                    // Top
                    borderLightWrite[baseOffset + 3 * chunkWidth + i] = (byte)GetAvailableLight(i, chunkWidth - 1);
                }
            }
        }

        int ChunkIndex(int2 chunk)
        {
            for (int index = chunks.Length - 1; index >= 0; index--)
            {
                if (chunks[index].Equals(chunk)) return index;
            }
            return -1;
        }
    }

    [BurstCompile]
    partial struct CopyLightJob : IJobParallelFor
    {
        public int chunkWidth;
        [ReadOnly]
        public NativeArray<int2> chunks;
        [ReadOnly]
        public NativeArray<byte> light;

        [NativeDisableParallelForRestriction]
        public RealmData realmData;

        [WriteOnly]
        public NativeQueue<LightUpdateInfo>.ParallelWriter lightUpdates;

        public void Execute(int index)
        {
            var chunkPos = chunks[index];
            if (!realmData.TryGetChunk(chunkPos, out var chunk)) return;
            var lightChunk = light.GetChunk(index, chunkWidth * chunkWidth);
            for (int x = 0; x < chunkWidth; x++)
            {
                for (int y = 0; y < chunkWidth; y++)
                {
                    var light = lightChunk.GetElement2d(x, y, chunkWidth);
                    if (chunk.OverwriteLight(x, y, light))
                    {
                        lightUpdates.Enqueue(new()
                        {
                            light = light,
                            pos = math.int2(x, y) + chunkPos * chunkWidth
                        });
                    }
                }
            }
        }
    }
}

public struct LightUpdateInfo
{
    public int2 pos;
    public byte light;
}

public struct LightJobInfo
{
    public NativeArray<byte> light;
    public JobHandle jobHandle;
    public NativeArray<int2> updatedChunks;

    public JobHandle Dispose(JobHandle dep)
    {
        return JobHandle.CombineDependencies(light.Dispose(dep), updatedChunks.Dispose(dep));
    }

    public void Dispose()
    {
        jobHandle.Complete();
        if(light.IsCreated) light.Dispose();
        if(updatedChunks.IsCreated) updatedChunks.Dispose();
    }
}
