using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWallBlock", menuName = "Block/Wall", order = 1)]
public class Wall : Block
{
    /// <summary>
    /// Can support roofs
    /// </summary>
    public bool structural = false;

    /// <summary>
    /// Counts in enclosure calculations
    /// </summary>
    public bool solid = false;

    public bool Walkable = false;

    public List<Ground> MustBePlacedOn;  
}
