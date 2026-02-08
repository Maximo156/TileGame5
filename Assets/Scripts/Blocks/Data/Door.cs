using BlockDataRepos;
using NativeRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDoorBlock", menuName = "Block/DoorBlock", order = 1)]
public class Door : Wall, IInteractableBlock
{
    public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice)
    {
        slice.simpleBlockState = slice.simpleBlockState == 0 ? (byte)1 : (byte)0;
        return true;
    }
}
