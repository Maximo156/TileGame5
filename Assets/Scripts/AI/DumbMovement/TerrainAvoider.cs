using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainAvoider : SteeringBehavior
{
    protected override (float[] interests, float[] danger) GetWeightsImpl(Vector2[] Directions, float[] interests, float[] danger, ContextSteerer Steerer)
    {
        var curBlock = Utilities.GetBlockPos(Steerer.transform.position);
        danger = Directions.Select(v => ChunkManager.GetBlock(BlockOffset(v) + curBlock).Walkable ? 0f : 1).Select((d, i) => Mathf.Max(d, danger[i])).ToArray();
        return (interests, danger);
    }

    Vector2Int BlockOffset(Vector2 dir)
    {
        return new Vector2Int(Mathf.Abs(dir.x) < 0.01 ? 0 : (int)Mathf.Sign(dir.x), Mathf.Abs(dir.y) < 0.01 ? 0 : (int)Mathf.Sign(dir.y));
    }
}
