using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class InteractiveDislay : MonoBehaviour
{
    public abstract Type TypeMatch();
    public abstract void DisplayInventory(Vector2Int worldPos, BlockSlice slice, IInventoryContainer otherInventory);
    public abstract void Detach();
}

public class InteractiveDisplayController : MonoBehaviour
{
    [Serializable]
    public class Display
    {
        public InteractiveDislay display;
        public int priority;
    }

    public Camera Cam;

    public List<Display> displays;

    void Awake()
    {
        Chunk.OnBlockChanged += BlockChanged;
        PlayerMouseInput.OnBlockInterfaced += OnInteract;
        gameObject.SetActive(false);
        foreach (var display in displays)
        {
            display.display.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        displays = displays.OrderBy(d => d.priority).ToList();
    }

    private void Update()
    {
        if (curPos != null)
        {
            transform.position = Cam.WorldToScreenPoint(curPos.Value.ToVector3Int());
        }
    }

    Vector2Int? curPos;
    Block curBlock;
    private void OnInteract(Vector2Int pos, BlockSlice slice, IInventoryContainer userInventory)
    {
        if(curPos == pos)
        {
            Close();
            return;
        }
        Detach();
        curPos = pos;
        curBlock = slice.WallBlock;
        gameObject.SetActive(true);
        bool found = false;
        foreach (var display in displays.Select(d => d.display))
        {
            display.gameObject.SetActive(false);
            if (!found && display.TypeMatch().IsAssignableFrom(slice.WallBlock.GetType()))
            {
                display.gameObject.SetActive(true);
                display.DisplayInventory(pos, slice, userInventory);
                found = true;
            }
        }
        if (!found)
        {
            Detach();
        }
        else
        {
            transform.position = Cam.WorldToScreenPoint(pos.ToVector3Int());
        }
    }

    private void BlockChanged(Chunk _, Vector2Int BlockPos, Vector2Int __, BlockSlice block)
    {
        if(BlockPos == curPos && curBlock != block.WallBlock)
        {
            Detach();
            Close();
        }
    }

    private void Detach()
    {
        foreach(var display in displays)
        {
            display.display.Detach();
        }
    }

    private void Close()
    {
        gameObject.SetActive(false);
        curPos = null;
        curBlock = null;
        Detach();
    }
}
