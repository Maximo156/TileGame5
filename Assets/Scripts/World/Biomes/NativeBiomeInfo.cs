using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;


public struct NativeBiomeInfo
{
    NativeArray<NativeBiomePreset> Biomes;
    NativeArray<NativeBiomePreset> WallBiomes;
    NativeArray<NativeSparceInfo> sparceBlocks;
    NativeArray<ReplacementInfo> replacementInfo;
    float waterLevel;

    public bool isCreated => WallBiomes.IsCreated && Biomes.IsCreated && sparceBlocks.IsCreated && replacementInfo.IsCreated;

    public NativeBiomeInfo(List<BiomePreset> biomes, List<BiomePreset> wallBiomes, float waterLevel)
    {
        Biomes = new NativeArray<NativeBiomePreset>(biomes.Count, Allocator.Persistent);
        WallBiomes = new NativeArray<NativeBiomePreset>(wallBiomes.Count, Allocator.Persistent);
        sparceBlocks = new NativeArray<NativeSparceInfo>(biomes.Sum(b => b.SparceBlockInfos.Count) + wallBiomes.Sum(b => b.SparceBlockInfos.Count), Allocator.Persistent);
        replacementInfo = new NativeArray<ReplacementInfo>(biomes.Sum(b => b.ReplacementInfos.Count), Allocator.Persistent);
        this.waterLevel = waterLevel;

        int count = 0;
        int sparceCount = 0;
        int replacementCount = 0;
        foreach (var biome in biomes)
        {
            var info = biome.GetNativePreset();
            info.sparceBlockSlice = new SliceData()
            {
                start = sparceCount,
                length = biome.SparceBlockInfos.Count
            };
            foreach (var block in biome.SparceBlockInfos)
            {
                sparceBlocks[sparceCount++] = new()
                {
                    block = block.block.Id,
                    blockLevel = BlockDataRepo.GetNativeBlock(block.block.Id).Level,
                    Weight = block.Weight,
                };
            }
            info.replacementBlockSlice = new SliceData()
            {
                start = replacementCount,
                length = biome.ReplacementInfos.Count
            };
            foreach (var block in biome.ReplacementInfos)
            {
                replacementInfo[replacementCount++] = new()
                {
                    Block = block.Original.Id,
                    ReplaceWith = block.Replacement.Id
                };
            }

            Biomes[count] = info;
            count++;
        }
        count = 0;

        foreach (var biome in wallBiomes)
        {
            var info = biome.GetNativePreset();
            info.sparceBlockSlice = new SliceData()
            {
                start = sparceCount,
                length = biome.SparceBlockInfos.Count
            };
            foreach (var block in biome.SparceBlockInfos)
            {
                sparceBlocks[sparceCount++] = new()
                {
                    block = block.block.Id,
                    blockLevel = BlockDataRepo.GetNativeBlock(block.block.Id).Level,
                    Weight = block.Weight,
                };
            }
            info.replacementBlockSlice = new SliceData()
            {
                start = replacementCount,
                length = biome.ReplacementInfos.Count
            };
            foreach (var block in biome.ReplacementInfos)
            {
                replacementInfo[replacementCount++] = new()
                {
                    Block = block.Original.Id,
                    ReplaceWith = block.Replacement.Id
                };
            }

            WallBiomes[count] = info;
            count++;
        }
    }

    public void Dispose()
    {
        if (Biomes.IsCreated) Biomes.Dispose();
        if (WallBiomes.IsCreated) WallBiomes.Dispose();
        if (sparceBlocks.IsCreated) sparceBlocks.Dispose();
        if (replacementInfo.IsCreated) replacementInfo.Dispose();
    }

    public readonly bool TryGetWall(float height, out NativeBiomePreset wallBiome)
    {
        for (int i = WallBiomes.Length - 1; i >= 0; i--)
        {
            var w = WallBiomes[i];
            if (height > w.minHeight)
            {
                wallBiome = w;
                return true;
            }
        }

        wallBiome = default;
        return false;
    }

    public readonly bool TryGetBiome(float height, float moisture, float heat, out NativeBiomePreset biome)
    {
        biome = default;

        if (height <= waterLevel)
            return false;

        float bestScore = float.MaxValue;
        bool found = false;

        for (int i = 0; i < Biomes.Length; i++)
        {
            var b = Biomes[i];

            if (!b.MatchCondition(moisture, heat))
                continue;

            float diff = b.GetDiffValue(moisture, heat);
            if (diff < bestScore)
            {
                bestScore = diff;
                biome = b;
                found = true;
            }
        }

        return found;
    }

    public NativeSlice<NativeSparceInfo> GetSparceBlocks(NativeBiomePreset biome)
    {
        return new NativeSlice<NativeSparceInfo>(sparceBlocks, biome.sparceBlockSlice.start, biome.sparceBlockSlice.length);
    }

    public ushort GetReplacementInfo(NativeBiomePreset preset, ushort toReplace)
    {
        var slice = new NativeSlice<ReplacementInfo>(replacementInfo, preset.replacementBlockSlice.start, preset.replacementBlockSlice.length);
        for (int i = 0; i < slice.Length; i++)
        {
            var r = slice[i];
            if (r.Block == toReplace)
            {
                return r.ReplaceWith;
            } 
        }
        return toReplace;
    }

    struct ReplacementInfo
    {
        public ushort Block;
        public ushort ReplaceWith;
    }
}

public struct NativeBiomePreset
{
    public int localId;
    public ushort groundBlock;
    public ushort wallBlock;
    public ushort roofBlock;

    public float minHeight;
    public float minMoisture;
    public float minHeat;

    public float sparceDesnity;
    public bool sparceReplaceSolid;

    public SliceData sparceBlockSlice;
    public SliceData replacementBlockSlice;

    public bool MatchCondition(float moisture, float heat)
    {
        return moisture >= minMoisture && heat >= minHeat;
    }

    public float GetDiffValue(float moisture, float heat)
    {
        return (moisture - minMoisture) + (heat - minHeat);
    }
}
