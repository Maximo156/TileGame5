using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoofBlock", menuName = "Block/Roof", order = 1)]
public class Roof : Block
{
    public int Strength  = 4;

    public override BlockData GetBlockData()
    {
        var data = base.GetBlockData();
        data.roofStrength = Strength;
        return data;
    }
}
