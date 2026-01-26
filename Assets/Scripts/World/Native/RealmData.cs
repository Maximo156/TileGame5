using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NativeRealm {
    [BurstCompile]
    public struct RealmData
    {
        int chunkWidth;
        int chunkDataLength;

        NativeArray<ushort> groundBlocks;
        NativeArray<ushort> wallBlocks;
        NativeArray<ushort> roofBlocks;

        NativeArray<byte> simpleBlockState;
        NativeArray<byte> lightLevel;
        NativeArray<bool> water;

        NativeList<int> emptySectors;
        NativeHashMap<int2, ChunkMetadata> metadata;

        public RealmData(int chunkWidth, int totalExpectedChunks, Allocator allocator)
        {
            this.chunkWidth = chunkWidth;
            chunkDataLength = chunkWidth * chunkWidth;

            int totalLength = chunkDataLength * totalExpectedChunks;

            groundBlocks = new NativeArray<ushort>(totalLength, allocator);
            wallBlocks = new NativeArray<ushort>(totalLength, allocator);
            simpleBlockState = new NativeArray<byte>(totalLength, allocator);
            lightLevel = new NativeArray<byte>(totalLength, allocator);
            roofBlocks = new NativeArray<ushort>(totalLength, allocator);
            water = new NativeArray<bool>(totalLength, allocator);

            metadata = new NativeHashMap<int2, ChunkMetadata>(totalExpectedChunks, allocator);
            emptySectors = new NativeList<int>(totalExpectedChunks, allocator);
        }

        public RealmData(int chunkWidth, int totalExpectedChunks)
            : this(chunkWidth, totalExpectedChunks, Allocator.Persistent) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkData AddChunk(int2 chunkPos)
        {
            // If it already exists, just return it
            if (metadata.TryGetValue(chunkPos, out var existingMeta))
            {
                int startExisting = existingMeta.StartingPos * chunkDataLength;
                return new ChunkData(
                    chunkWidth,
                    new NativeSlice<ushort>(groundBlocks, startExisting, chunkDataLength),
                    new NativeSlice<ushort>(wallBlocks, startExisting, chunkDataLength),
                    new NativeSlice<byte>(simpleBlockState, startExisting, chunkDataLength),
                    new NativeSlice<byte>(lightLevel, startExisting, chunkDataLength),
                    new NativeSlice<ushort>(roofBlocks, startExisting, chunkDataLength),
                    new NativeSlice<bool>(water, startExisting, chunkDataLength)
                );
            }

            int sector;

            if (emptySectors.Length > 0)
            {
                sector = emptySectors[emptySectors.Length - 1];
                emptySectors.RemoveAtSwapBack(emptySectors.Length - 1);
            }
            else
            {
                sector = metadata.Count();
            }

            metadata.Add(chunkPos, new ChunkMetadata
            {
                StartingPos = sector
            });

            int start = sector * chunkDataLength;

            return new ChunkData(
                chunkWidth,
                new NativeSlice<ushort>(groundBlocks, start, chunkDataLength),
                new NativeSlice<ushort>(wallBlocks, start, chunkDataLength),
                new NativeSlice<byte>(simpleBlockState, start, chunkDataLength),
                new NativeSlice<byte>(lightLevel, start, chunkDataLength),
                new NativeSlice<ushort>(roofBlocks, start, chunkDataLength),
                new NativeSlice<bool>(water, start, chunkDataLength)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChunk(int2 chunkPos)
        {
            if (!metadata.TryGetValue(chunkPos, out var meta))
                return;

            int sector = meta.StartingPos;

            metadata.Remove(chunkPos);
            emptySectors.Add(sector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetChunk(int2 chunkPos, out ChunkData chunk)
        {
            if (!metadata.TryGetValue(chunkPos, out var meta))
            {
                chunk = default;
                return false;
            }

            int start = meta.StartingPos * chunkDataLength;

            chunk = new ChunkData(
                chunkWidth,
                new NativeSlice<ushort>(groundBlocks, start, chunkDataLength),
                new NativeSlice<ushort>(wallBlocks, start, chunkDataLength),
                new NativeSlice<byte>(simpleBlockState, start, chunkDataLength),
                new NativeSlice<byte>(lightLevel, start, chunkDataLength),
                new NativeSlice<ushort>(roofBlocks, start, chunkDataLength),
                new NativeSlice<bool>(water, start, chunkDataLength)
            );

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkData GetChunk(int2 chunkPos)
        {
            if (!metadata.TryGetValue(chunkPos, out var meta))
            {
                throw new System.Exception($"Chunk {chunkPos} not found");
            }

            int start = meta.StartingPos * chunkDataLength;

            return new ChunkData(
                chunkWidth,
                new NativeSlice<ushort>(groundBlocks, start, chunkDataLength),
                new NativeSlice<ushort>(wallBlocks, start, chunkDataLength),
                new NativeSlice<byte>(simpleBlockState, start, chunkDataLength),
                new NativeSlice<byte>(lightLevel, start, chunkDataLength),
                new NativeSlice<ushort>(roofBlocks, start, chunkDataLength),
                new NativeSlice<bool>(water, start, chunkDataLength)
            );
        }

        public void Dispose()
        {
            groundBlocks.Dispose();
            wallBlocks.Dispose();
            simpleBlockState.Dispose();
            lightLevel.Dispose();
            roofBlocks.Dispose();
            emptySectors.Dispose();
            metadata.Dispose();
            water.Dispose();
        }

        public JobHandle Dispose(JobHandle dep)
        {
            return JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    JobHandle.CombineDependencies(
                        groundBlocks.Dispose(dep),
                        wallBlocks.Dispose(dep)
                    ),
                    JobHandle.CombineDependencies(
                       simpleBlockState.Dispose(dep),
                       lightLevel.Dispose(dep)
                    )
                ),
                JobHandle.CombineDependencies(
                    JobHandle.CombineDependencies(
                        roofBlocks.Dispose(dep),
                        emptySectors.Dispose(dep)
                    ),
                    JobHandle.CombineDependencies(
                        metadata.Dispose(dep),
                        water.Dispose(dep)
                    )
                )
            );
        }

        public struct ChunkMetadata
        {
            public int StartingPos;
        }

        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(chunkWidth, groundBlocks, wallBlocks, roofBlocks, simpleBlockState, lightLevel, water, metadata.AsParallelWriter());
        }

        ParallelChunkWriter AsParallelChunkWriter()
        {
            return new ParallelChunkWriter(chunkWidth, groundBlocks, wallBlocks, roofBlocks, simpleBlockState, lightLevel, water);
        }

        public JobHandle CopyFrom(RealmData otherData, NativeArray<int2> chunks, JobHandle dep)
        {
            foreach(var c  in chunks)
            {
                AddChunk(c);
            }
            return new CopyJob()
            {
                otherData = otherData,
                target = AsParallelChunkWriter(),
                chunks = chunks,
                chunkData = metadata
            }.Schedule(chunks.Length, 1, dep);
        }

        [BurstCompile]
        public struct ParallelWriter
        {
            NativeArray<ushort> groundBlocks;
            NativeArray<ushort> wallBlocks;
            NativeArray<ushort> roofBlocks;

            NativeArray<byte> simpleBlockState;
            NativeArray<byte> lightLevel;
            NativeArray<bool> water;

            NativeHashMap<int2, ChunkMetadata>.ParallelWriter metadata;

            int chunkLength;
            int chunkWidth;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ParallelWriter(
                int chunkWidth,
                NativeArray<ushort> groundBlocks,
                NativeArray<ushort> wallBlocks,
                NativeArray<ushort> roofBlocks,
                NativeArray<byte> simpleBlockState,
                NativeArray<byte> lightLevel,
                NativeArray<bool> water,
                NativeHashMap<int2, ChunkMetadata>.ParallelWriter metadata
            )
            {
                this.chunkWidth = chunkWidth;
                this.chunkLength = chunkWidth * chunkWidth;

                this.groundBlocks = groundBlocks;
                this.wallBlocks = wallBlocks;
                this.roofBlocks = roofBlocks;

                this.simpleBlockState = simpleBlockState;
                this.lightLevel = lightLevel;
                this.water = water;

                this.metadata = metadata;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ChunkData InitChunk(int2 pos, int index)
            {
                int start = index * chunkLength;

                metadata.TryAdd(pos, new ChunkMetadata
                {
                    StartingPos = index
                });

                return new ChunkData(
                    chunkWidth,
                    groundBlocks.Slice(start, chunkLength),
                    wallBlocks.Slice(start, chunkLength),
                    simpleBlockState.Slice(start, chunkLength),
                    lightLevel.Slice(start, chunkLength),
                    roofBlocks.Slice(start, chunkLength),
                    water.Slice(start, chunkLength)
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ChunkData GetChunk(int index)
            {
                int start = index * chunkLength;

                return new ChunkData(
                    chunkWidth,
                    groundBlocks.Slice(start, chunkLength),
                    wallBlocks.Slice(start, chunkLength),
                    simpleBlockState.Slice(start, chunkLength),
                    lightLevel.Slice(start, chunkLength),
                    roofBlocks.Slice(start, chunkLength),
                    water.Slice(start, chunkLength)
                );
            }
        }

        [BurstCompile]
        struct ParallelChunkWriter
        {
            NativeArray<ushort> groundBlocks;
            NativeArray<ushort> wallBlocks;
            NativeArray<ushort> roofBlocks;

            NativeArray<byte> simpleBlockState;
            NativeArray<byte> lightLevel;
            NativeArray<bool> water;

            int chunkWidth;
            int chunkLength;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ParallelChunkWriter(
                int chunkWidth,
                NativeArray<ushort> groundBlocks,
                NativeArray<ushort> wallBlocks,
                NativeArray<ushort> roofBlocks,
                NativeArray<byte> simpleBlockState,
                NativeArray<byte> lightLevel,
                NativeArray<bool> water
            )
            {
                this.chunkWidth = chunkWidth;
                this.chunkLength = chunkWidth * chunkWidth;

                this.groundBlocks = groundBlocks;
                this.wallBlocks = wallBlocks;
                this.roofBlocks = roofBlocks;

                this.simpleBlockState = simpleBlockState;
                this.lightLevel = lightLevel;
                this.water = water;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ChunkData GetChunk(int index)
            {
                int start = index * chunkLength;

                return new ChunkData(
                    chunkWidth,
                    groundBlocks.Slice(start, chunkLength),
                    wallBlocks.Slice(start, chunkLength),
                    simpleBlockState.Slice(start, chunkLength),
                    lightLevel.Slice(start, chunkLength),
                    roofBlocks.Slice(start, chunkLength),
                    water.Slice(start, chunkLength)
                );
            }
        }


        partial struct CopyJob : IJobParallelFor
        {
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public RealmData otherData;

            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public ParallelChunkWriter target;

            [ReadOnly]
            public NativeArray<int2> chunks;

            [ReadOnly]
            public NativeHashMap<int2, ChunkMetadata> chunkData;

            public void Execute(int index)
            {
                var chunk = chunks[index];
                var metadata = chunkData[chunk];
                var targetChunk = target.GetChunk(metadata.StartingPos);
                otherData.TryGetChunk(chunk, out var sourceChunk);
                targetChunk.CopyFrom(sourceChunk);
            }
        }
    }

    [BurstCompile]
    public struct ChunkData
    {
        public int chunkWidth;

        NativeSlice<ushort> groundBlocks;
        NativeSlice<ushort> wallBlocks;
        NativeSlice<byte> simpleBlockState;
        NativeSlice<byte> lightLevel;
        NativeSlice<ushort> roofBlocks;
        NativeSlice<bool> water;

        public ChunkData(
            int chunkWidth,
            NativeSlice<ushort> groundBlocks,
            NativeSlice<ushort> wallBlocks,
            NativeSlice<byte> simpleBlockState,
            NativeSlice<byte> lightLevel,
            NativeSlice<ushort> roofBlocks,
            NativeSlice<bool> water
        )
        {
            this.chunkWidth = chunkWidth;
            this.groundBlocks = groundBlocks;
            this.wallBlocks = wallBlocks;
            this.simpleBlockState = simpleBlockState;
            this.lightLevel = lightLevel;
            this.roofBlocks = roofBlocks;
            this.water = water;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int x, int y)
        {
            return x + y * chunkWidth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeBlockSlice GetSlice(int x, int y)
        {
            int i = Index(x, y);
            return new NativeBlockSlice
            {
                groundBlock = groundBlocks[i],
                wallBlock = wallBlocks[i],
                simpleBlockState = simpleBlockState[i],
                lightLevel = lightLevel[i],
                roofBlock = roofBlocks[i],
                isWater = water[i]
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetGround(int x, int y)
        {
            int i = Index(x, y);
            return groundBlocks[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetLight(int x, int y)
        {
            int i = Index(x, y);
            return lightLevel[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetWall(int x, int y)
        {
            int i = Index(x, y);
            return wallBlocks[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetRoof(int x, int y)
        {
            int i = Index(x, y);
            return roofBlocks[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetIsWater(int x, int y)
        {
            int i = Index(x, y);
            return water[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlock(int x, int y, NativeBlockSlice value)
        {
            int i = Index(x, y);
            groundBlocks[i] = value.groundBlock;
            wallBlocks[i] = value.wallBlock;
            roofBlocks[i] = value.roofBlock;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetWall(int x, int y, ushort value)
        {
            int i = Index(x, y);
            wallBlocks[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloor(int x, int y, ushort value)
        {
            int i = Index(x, y);
            groundBlocks[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRoof(int x, int y, ushort value)
        {
            int i = Index(x, y);
            roofBlocks[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetState(int x, int y, byte value)
        {
            int i = Index(x, y);
            simpleBlockState[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeSlice(int x, int y, NativeBlockSlice value)
        {
            int i = Index(x, y);
            groundBlocks[i] = value.groundBlock;
            wallBlocks[i] = value.wallBlock;
            roofBlocks[i] = value.roofBlock;
            simpleBlockState[i] = value.simpleBlockState;
            lightLevel[i] = value.lightLevel;   
            water[i] = value.isWater;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ChunkData otherChunk)
        {
            groundBlocks.CopyFrom(otherChunk.groundBlocks);
            wallBlocks.CopyFrom(otherChunk.wallBlocks);
            simpleBlockState.CopyFrom(otherChunk.simpleBlockState);
            lightLevel.CopyFrom(otherChunk.lightLevel);
            roofBlocks.CopyFrom(otherChunk.roofBlocks);
            water.CopyFrom(otherChunk.water);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OverwriteLight(int x, int y, byte newLight)
        {
            var oldLight = lightLevel.GetElement2d(x, y, chunkWidth);
            lightLevel.SetElement2d(x, y, chunkWidth, newLight);
            return oldLight != newLight;
        }
    }

    public struct NativeBlockSlice
    {
        public ushort groundBlock;
        public ushort wallBlock;
        public ushort roofBlock;
        public byte simpleBlockState;
        public byte lightLevel;
        public bool isWater;
    }
}
