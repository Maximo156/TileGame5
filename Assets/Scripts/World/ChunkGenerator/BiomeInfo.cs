using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBiomeInfo", menuName = "Terrain/BiomeInfo", order = 1)]
public class BiomeInfo : ScriptableObject
{
    [Serializable]
    public class Layer
    {
        public float minHeight;
        public List<Ground> GroundBlocks;
        public List<Wall> WallBlocks;

        [Header("Sparce Blocks")]
        public bool replaceSolid = false;
        public List<Block> SparceBlocks;
        public SoundSettings SparceSoundSettings;
        public float SparceDensity;
    }

    public SoundSettings soundSettings;
    public List<Layer> Layers;

    public float waterLevel;

    public BiomeBlockInfo GetInfo(Vector2Int worldPos)
    {
        BiomeBlockInfo res = new();
        res.height = soundSettings.GetSound(worldPos.x, worldPos.y);
        if (res.height > waterLevel)
        {
            res.Water = false;
        }
        res.layer = Layers.FirstOrDefault(l => res.height > l.minHeight);
        return res;
    }

    private void OnValidate()
    {
        Layers = Layers.OrderByDescending(l => l.minHeight).ToList();
    }

    public struct BiomeBlockInfo
    {
        public bool Water;
        public float height;
        public Layer layer;
    }
}
