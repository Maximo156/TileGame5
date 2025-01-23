using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStorageBlock", menuName = "Block/StorageBlock", order = 1)]
public class StorageBlock : Wall, IInterfaceBlock
{
    public int size;
    
    public override BlockState GetState()
    {
        return new StorageState(size);
    }
}

public class StorageState : BlockState
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
}