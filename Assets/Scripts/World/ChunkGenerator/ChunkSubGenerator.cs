using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class ChunkSubGenerator: ScriptableObject
{
    public int Priority;
    public abstract Task UpdateBlockSlices(BlockSlice[,] blocks, Vector2Int ChunkPosition, Vector2Int WorldPosition, BiomeInfo biomeInfo, System.Random rand, GenerationCache cache);

}
