using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCrateBlock", menuName = "Block/CrateBlock", order = 1)]
public class CrateBlock : Wall, IStatefulBlock
{
    public BlockState GetState()
    {
        return new CrateState();
    }
}

public class CrateState : BlockState, IStorageBlockState
{
    public List<ItemStack> AdditionalDrops;

    public bool AddItemStack(ItemStack stack)
    {
        if(AdditionalDrops == null) 
        {
            AdditionalDrops = new List<ItemStack>();
        }
        AdditionalDrops.Add(stack);
        return true;
    }

    public override void CleanUp(Vector2Int pos)
    {
        if(AdditionalDrops is not null)
        {
            Utilities.DropItems(pos, AdditionalDrops);
        }
    }
}
