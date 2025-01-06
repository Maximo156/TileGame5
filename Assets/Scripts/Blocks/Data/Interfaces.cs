using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractableState
{
}

public interface IInterfaceBlock
{
}

public interface IInteractableBlock
{
    public bool Interact(Vector2Int worldPos, BlockSlice slice);
}
