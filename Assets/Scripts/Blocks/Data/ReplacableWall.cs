using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewReplacableWallBlock", menuName = "Block/ReplacableWall", order = 1)]
public class ReplacableBlock : Wall, ITickableBlock
{
    public Wall nextBlock;
    public int MeanSecondsToHappen;

    protected virtual Wall NewBlock()
    {
        return nextBlock;
    }

    public bool Tick(Vector2Int worlPosition, BlockSlice slice, System.Random rand)
    {
        if (rand.NextDouble() < 1 - Mathf.Pow((float)System.Math.E, -1f * ChunkManager.MsPerTick / (1000 * MeanSecondsToHappen)))
        {
            slice.SetBlock(NewBlock());
            return true;
        }
        return false;
    }
}
