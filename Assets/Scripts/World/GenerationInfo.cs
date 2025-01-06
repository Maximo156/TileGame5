using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class GenerationInfo
{/*
    [Serializable]
    public class LayerInfo
    {
        public float minHeight;
        [Header("Ground Info")]
        public SingleTileMap map;

        [Header("Block Info")]
        public BlockV2 block;
        public float SpecialDensity;
        public List<BlockV2> SpecialBlocks;
        public List<BlockV2> SpecialPlacables;

        [Header("Shadow")]
        public bool genShadow;

        public float MovementModifier = 1;
    }

    public List<LayerInfo> layers;
    public SingleTileMap shadow;
    public SoundSettings heightSound;
    public SoundSettings caveSound;
    [Range(0, 1)]
    public float caveCutoff;
    public SoundSettings densitySound;
    public int ChunkWidth;
    public float chunkChanceOfTown;
    */
    public int GetLayer(int x, int y)
    {
        //float height = heightSound.GetSound(x, y);
        return 1;// layers.FindLastIndex(l => l.minHeight < height);
    }
}
