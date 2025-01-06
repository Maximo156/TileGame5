using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RefreshRuleTile<T> : RefreshRuleTile
{
    /// <summary>
    /// Returns the Neighbor Rule Class type for this Rule Tile.
    /// </summary>
    public sealed override Type m_NeighborType => typeof(T);
}

[CreateAssetMenu(fileName = "NewRefreshTile", menuName = "Tile/RefreshTile", order = 1)]
public class RefreshRuleTile : RuleTile
{
    public override void RefreshTile(Vector3Int location, ITilemap tilemap)
    {
        tilemap.RefreshTile(location);
        foreach (var v in Utilities.OctAdjacent3.Select(v => v + location))
        {
            tilemap.RefreshTile(v);
        }
    }
}
