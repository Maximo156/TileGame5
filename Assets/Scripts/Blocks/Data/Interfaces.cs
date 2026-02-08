using NativeRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public interface IInteractableState
{
}

public interface IStatefulBlock
{
    public BlockState GetState();
}

public interface IInterfaceBlock
{
}

public interface IInteractableBlock
{
    public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice);
}

public interface IStorageBlockState
{
    public bool AddItemStack(ItemStack stack);
}
