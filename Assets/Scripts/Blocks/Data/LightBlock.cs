using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLightBlock", menuName = "Block/LightBlock", order = 1)]
public class LightBlock : Wall
{
    [Header("Light Settings")]
    public byte LightLevel;

    public override BlockData GetBlockData()
    {
        var data = base.GetBlockData();
        data.lightLevel = (byte)(LightLevel * 2); // Double level internally as light decreases 2 levels/block
        return data;
    }
}
