using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickableBlock
{
    public ushort Tick(Vector2Int worlPosition, BlockState state, System.Random rand);
}
