using NativeRealm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

[BurstCompile]
[CreateAssetMenu(fileName = "NewStructureGenerator2", menuName = "Terrain/StructureGenerator2", order = 1)]
public class StructureGenerator2 : ChunkSubGenerator
{
    private struct Rotation
    {
        public static readonly Rotation zero = new();
        public static readonly Rotation ninety = new() { invX = true, mirror = true, dif = 1 };
        public static readonly Rotation oneeighty = new() { invX = true, invY = true, dif = 2 };
        public static readonly Rotation twoseventy = new() { invY = true, mirror = true, dif = 3 };

        public bool invX;
        public bool invY;
        public bool mirror;
        public int dif; 
    }

    public override void UpdateRequestedChunks(NativeList<int2> chunks, RealmInfo info)
    {
        var structChunkWidth = info.StructureInfo.StructureChunkWidth;
        var invRatio = structChunkWidth * 1f / WorldSettings.ChunkWidth;
        var StructureChunks = GetStructureChunks(chunks.AsArray(), structChunkWidth);

        var newChunks = new HashSet<int2>();
        foreach(var c in StructureChunks)
        {
            var minChunk = math.int2(math.floor((math.float2(c) - math.float2(0.5f)) * invRatio));
            var maxChunk = math.int2(math.ceil((math.float2(c) + math.float2(1.5f)) * invRatio));
            for(int x = minChunk.x; x < maxChunk.x; x++)
            {
                for(int y = minChunk.y; y < maxChunk.y; y++)
                {
                    newChunks.Add(math.int2(x, y));
                }
            }
        }
        chunks.ResizeUninitialized(newChunks.Count);
        var count = 0;
        foreach(var c in newChunks)
        {
            chunks[count++] = c;
        }
    }

    private HashSet<int2> GetStructureChunks(NativeArray<int2> originalChunks, int structChunkWidth)
    {
        var StructureChunks = new HashSet<int2>();
        var ratio = WorldSettings.ChunkWidth * 1f / structChunkWidth;
        foreach (var c in originalChunks)
        {
            StructureChunks.Add(math.int2(math.floor(math.float2(c) * ratio)));
        }
        return StructureChunks;
    }

    public override JobHandle ScheduleGeneration(int chunkWidth, NativeArray<int2> originalChunks, NativeArray<int2> requestChunks, RealmData realmData, RealmInfo realmInfo, ref BiomeData biomeData, JobHandle dep = default)
    {
        var structChunkWidth = realmInfo.StructureInfo.StructureChunkWidth;
        var chunkHash = GetStructureChunks(originalChunks, structChunkWidth);
        var structureChunks = new NativeArray<int2>(chunkHash.Count, Allocator.Persistent);
        var i = 0;
        foreach(var c in chunkHash)
        {
            structureChunks[i++] = c;
        }

        var genStructures = new StructureGenerationJob()
        {
            structureChunkWidth = structChunkWidth,
            chunkWidth = chunkWidth,
            structureChunks = structureChunks,
            chunks = requestChunks,
            biomeInfo = realmInfo.BiomeInfo.BiomeInfo,
            biomeData = biomeData,
            structureInfo = realmInfo.StructureInfo.StructureInfo,
            realmData = realmData,
        }.Schedule(structureChunks.Length, 1, dep);

        var cleanup = structureChunks.Dispose(genStructures);

        return cleanup;
    }

    [BurstCompile]
    partial struct StructureGenerationJob : IJobParallelFor
    {
        public int structureChunkWidth;
        public int chunkWidth;

        [ReadOnly] 
        public NativeArray<int2> structureChunks;

        [ReadOnly]
        public NativeArray<int2> chunks;

        [ReadOnly]
        public NativeBiomeInfo biomeInfo;

        [ReadOnly]
        public BiomeData biomeData;

        [ReadOnly]
        public NativeStructureInfo structureInfo;

        [NativeDisableParallelForRestriction]
        public RealmData realmData;

        public void Execute(int index)
        {
            var debug = false;

            var chunkWidth = this.chunkWidth;
            var structureChunks = this.structureChunks;
            var realmData = this.realmData;
            var chunks = this.chunks;
            var biomeData = this.biomeData;
            var biomeInfo = this.biomeInfo;
            var structureInfo = this.structureInfo;

            var structChunk = structureChunks[index];
            if (debug && !structChunk.Equals(int2.zero)) return;

            var (point, rand, structure) = RealmStructureInfo.GenStructureInfo(structChunk, structureChunkWidth, structureInfo);

            if (debug) point = int2.zero;

            var adjacentPoints = new NativeArray<int2>(8, Allocator.Temp);
            var placedBounds = new NativeList<BoundsInt>(structure.maxComponentCount, Allocator.Temp);
            var openAnchors = new NativeList<NativeComponentAnchor>(structure.maxComponentCount, Allocator.Temp);

            RealmStructureInfo.PopulateAdjacentPoints(structChunk, structureChunkWidth, adjacentPoints);

            var center = structureInfo.GetStructureCenterComponents(structure).SelectRandomWeighted(ref rand);
            var components = structureInfo.GetStructureBuildingComponents(structure);
            if (!BuildingFits(point, Rotation.zero, center))
            {
                Debug.LogWarning($"Inable to place structure at {point.x} {point.y}");
            }
            else
            {
                PlaceBuilding(point, Rotation.zero, center, AnchorDirection.None);
                FillAnchors();
            }

            adjacentPoints.Dispose();
            placedBounds.Dispose();
            openAnchors.Dispose();  
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool isValidBlockPos(int2 blockPos)
            {
                var dist = math.distancesq(point, blockPos);
                for(int i = 0; i< 8; i++)
                {
                    if (math.distancesq(adjacentPoints[i], blockPos) < dist)
                    {
                        return false;
                    }
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool BuildingFits(int2 start, Rotation rotation, NativeStructureComponent structure)
            {
                var bounds = !rotation.mirror ? structure.Bounds : new BoundsInt(Vector3Int.zero, new Vector3Int(structure.Bounds.size.y, structure.Bounds.size.x, structure.Bounds.size.z));
                bounds.position = start.ToVector().ToVector3Int();
                foreach(var b in placedBounds)
                {
                    if(b.Intersects(bounds)) return false;
                }
                foreach (var v in bounds.allPositionsWithin)
                {
                    var pos = v.ToVector2Int().ToInt();
                    if (!ValidLayer(pos, structure) || !isValidBlockPos(pos))
                    {
                        return false;
                    }
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool ValidLayer(int2 pos, NativeStructureComponent component)
            {
                var (chunk, localPos) = GetChunkAndPos(pos);
                var chunkHeight = biomeData.HeightMap.GetChunk(ChunkIndex(chunk), chunkWidth * chunkWidth);
                return !biomeInfo.TryGetWall(chunkHeight.GetElement2d(localPos.x, localPos.y, chunkWidth), out var _) || component.AllowMountains;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            (int2 chunk, int2 localPos) GetChunkAndPos(int2 pos)
            {
                var c = math.int2(math.floor(math.float2(pos) / chunkWidth));
                return (c, pos - (c * chunkWidth));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int ChunkIndex(int2 chunk)
            {
                for (int index = chunks.Length - 1; index >= 0; index--)
                {
                    if (chunks[index].Equals(chunk)) return index;
                }
                return -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void PlaceBuilding(int2 start, Rotation rotation, NativeStructureComponent component, AnchorDirection usedAnchorDir)
            {
                var compBounds = component.Bounds;
                var bounds = !rotation.mirror ? compBounds : new BoundsInt(Vector3Int.zero, new Vector3Int(compBounds.size.y, compBounds.size.x, compBounds.size.z));
                bounds.position = start.ToVector().ToVector3Int();
                var componentBlocks = structureInfo.GetComponentBlocks(component);
                var componentAnchors = structureInfo.GetComponentAnchors(component);
                placedBounds.Add(bounds);
                bool set = false;
                ChunkData chunk = default;
                int2 curChunkPos = default;
                foreach (var v in compBounds.allPositionsWithin)
                {
                    var pos = v.ToVector2Int().ToInt();
                    var rotatedPosition = ApplyRotation(rotation, pos, compBounds.size);
                    var worldPos = start + rotatedPosition;
                    var (chunkPos, localPos) = GetChunkAndPos(worldPos);
                    if(!set || !curChunkPos.Equals(chunkPos))
                    {
                        curChunkPos = pos;
                        chunk = realmData.GetChunk(chunkPos);
                    }

                    var block = componentBlocks.GetElement2d(pos.x, pos.y, compBounds.size.y);
                    var x = localPos.x;
                    var y = localPos.y;
                    if (block.groundBlock != 0)
                    {
                        chunk.SetFloor(x, y, block.groundBlock);
                        chunk.SetWall(x, y, block.wallBlock);
                    }
                    if(block.groundBlock != 0 || block.wallBlock != 0)
                    {
                        chunk.SetWall(x, y, block.wallBlock);
                    }
                    chunk.SetRoof(x, y, block.roofBlock);
                }
                foreach(var anchor in componentAnchors) 
                {
                    if (anchor.direction != AnchorDirection.None && anchor.direction != usedAnchorDir)
                    {
                        var rotatedPosition = ApplyRotation(rotation, anchor.offset, compBounds.size);
                        var worldPos = start + rotatedPosition;
                        var anchorInfo = new NativeComponentAnchor()
                        {
                            offset = worldPos,
                            Lock = anchor.Lock,
                            direction = (AnchorDirection)(((int)anchor.direction + rotation.dif) % 4),
                            key = anchor.key
                        };
                        openAnchors.Add(anchorInfo);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void FillAnchors()
            {
                int targetComponentCount = rand.NextInt(structure.minComponentCount, structure.maxComponentCount);
                int placedComponents = 0;
                NativeList<NativeStructureComponent> validComponents = new NativeList<NativeStructureComponent>(components.Length, Allocator.Temp);
                NativeList<NativeComponentAnchor> availAnchors = new NativeList<NativeComponentAnchor>(8, Allocator.Temp);
                while (openAnchors.Length > 0)
                {
                    validComponents.Clear();
                    var anchorIndex = rand.NextInt(openAnchors.Length);
                    var curAnchor = openAnchors[anchorIndex];
                    var anchorPos = curAnchor.offset;
                    openAnchors.RemoveAtSwapBack(anchorIndex);

                    foreach (var component in components)
                    {
                        var anchors = structureInfo.GetComponentAnchors(component);
                        foreach (var a in anchors)
                        {
                            if(a.Lock != curAnchor.Lock && a.key == curAnchor.key)
                            {
                                validComponents.Add(component); 
                                break;
                            }
                        }
                    }

                    while(validComponents.Length > 0)
                    {
                        var matchingComponentIndex = validComponents.SelectRandomIndexWeighted(ref rand);
                        var matchingComponent = validComponents[matchingComponentIndex];
                        validComponents.RemoveAtSwapBack(matchingComponentIndex);

                        var matchingSize = matchingComponent.Bounds.size;
                        bool placed = false;

                        var compAnchors = structureInfo.GetComponentAnchors(matchingComponent);
                        availAnchors.ResizeUninitialized(compAnchors.Length);
                        compAnchors.CopyTo(availAnchors.AsArray());
                        availAnchors.Shuffle(rand);

                        foreach (var matchingAnchor in availAnchors)
                        {
                            if (
                                matchingAnchor.direction == AnchorDirection.None ||
                                matchingAnchor.key != curAnchor.key ||
                                matchingAnchor.Lock == curAnchor.Lock
                                ) continue;
                            var rotationDif = Utilities.modulo(curAnchor.direction - 2 - matchingAnchor.direction, 4);
                            var rotation = rotationDif switch
                            {
                                0 => Rotation.zero,
                                1 => Rotation.ninety,
                                2 => Rotation.oneeighty,
                                3 => Rotation.twoseventy,
                                _ => throw new InvalidOperationException("Attempted to rotate more than 270")
                            };
                            var dirOffset = curAnchor.direction switch
                            {
                                AnchorDirection.Right => math.int2(1,0),
                                AnchorDirection.Up => math.int2(0, 1),
                                AnchorDirection.Left => math.int2(-1,0),
                                AnchorDirection.Down => math.int2(0, -1),
                                _ => throw new InvalidOperationException("Unknown anchor direction")
                            };

                            var anchorOffset = -ApplyRotation(rotation, matchingAnchor.offset, matchingSize);


                            if (BuildingFits((anchorPos + anchorOffset + dirOffset), rotation, matchingComponent))
                            {
                                PlaceBuilding((anchorPos + anchorOffset + dirOffset), rotation, matchingComponent, matchingAnchor.direction);
                                placed = true;
                                placedComponents++;
                                if (placedComponents >= targetComponentCount) return;
                                break;
                            }
                        }
                        if (placed) break;
                    }
                }
                availAnchors.Clear();
                validComponents.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int2 ApplyRotation(Rotation rot, int2 pos, Vector3Int size)
        {
            var res = new int2(!rot.invX ? pos.x : size.x - 1 - pos.x, !rot.invY ? pos.y : size.y - 1 - pos.y);
            return !rot.mirror ? res : new int2(res.y, res.x);
        }
    }
}
