using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDoorBlock", menuName = "Block/DoorBlock", order = 1)]
public class Door : Wall, IInteractableBlock
{
    public bool Interact(Vector2Int _, BlockSlice slice)
    {
        if(slice.State is DoorState door)
        {
            door.isOpen = !door.isOpen;
            return true;
        }
        return false;
    }

    public override BlockState GetState()
    {
        return new DoorState();
    }
}

public class DoorState : BlockState
{
    public bool isOpen;
    public override void CleanUp(Vector2Int pos)
    {
        
    }
}
