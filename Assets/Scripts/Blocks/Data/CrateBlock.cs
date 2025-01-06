using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCrateBlock", menuName = "Block/CrateBlock", order = 1)]
public class CrateBlock : Wall
{
    public override BlockState GetState()
    {
        return new CrateState();
    }
}

public class CrateState : BlockState
{
    public List<ItemStack> AdditionalDrops;
    public override void CleanUp(Vector2Int pos)
    {
        if(AdditionalDrops is not null)
        {
            Utilities.DropItems(pos, AdditionalDrops);
        }
    }
}
