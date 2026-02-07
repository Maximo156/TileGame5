using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStorageBlock", menuName = "Block/StorageBlock", order = 1)]
public class StorageBlock : Wall, IInterfaceBlock, IStatefulBlock
{
    public int size;

    public BlockState GetState()
    {
        return new StorageState(size);
    }
}

public class StorageState : BlockState, IStorageBlockState
{
    public Inventory StoredItems;

    public StorageState(int count)
    {
        StoredItems = new Inventory(count);
    }

    public override void CleanUp(Vector2Int pos)
    {
        Utilities.DropItems(pos, StoredItems.GetAllItems(false));
    }

    public bool AddItemStack(ItemStack stack)
    {
        return StoredItems.AddItem(stack);
    }
}