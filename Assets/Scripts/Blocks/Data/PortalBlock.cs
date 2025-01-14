using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPortalBlock", menuName = "Block/Portal", order = 1)]
public class PortalBlock : Wall, IInteractableBlock
{
    public delegate void PortalBlockUsed(string newDim, PortalBlock exitBlock, Vector2Int worldPos);
    public static event PortalBlockUsed OnPortalBlockUsed;

    [Header("Portal Info")]
    public string NewDim;
    public PortalBlock Exit;

    public bool Interact(Vector2Int worldPos, BlockSlice slice)
    {
        OnPortalBlockUsed?.Invoke(NewDim, Exit, worldPos);
        return false;
    }
}
