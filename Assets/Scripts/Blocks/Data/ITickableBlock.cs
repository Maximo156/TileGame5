using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickableBlock
{
    public bool Tick(Vector2Int worlPosition, BlockSlice slice, System.Random rand);
}
