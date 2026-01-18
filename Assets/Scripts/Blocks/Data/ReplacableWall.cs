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

    public ushort Tick(Vector2Int worlPosition, BlockState state, System.Random rand)
    {
        if (rand.NextDouble() < 1 - Mathf.Pow((float)System.Math.E, -1f * WorldSettings.TickMs / (1000 * MeanSecondsToHappen)))
        {
            return NewBlock().Id;
        }
        return 0;
    }
}
