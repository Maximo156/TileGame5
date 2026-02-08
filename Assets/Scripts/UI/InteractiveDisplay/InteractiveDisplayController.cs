using BlockDataRepos;
using NativeRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class InteractiveDislay : MonoBehaviour
{
    public abstract Type TypeMatch();
    public abstract void DisplayInventory(Vector2Int worldPos, Wall interfacedBlock, BlockState state, IInventoryContainer otherInventory);
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
        ChunkManager.OnRealmChange += OnRealmChange;
        PlayerMouseInput.OnBlockInterfaced += OnInteract;
        PlayerMouseInput.OnInterfaceRangeExceeded += Close;
        gameObject.SetActive(false);
        foreach (var display in displays)
        {
            display.display.gameObject.SetActive(false);
        }
    }

    void OnRealmChange(Realm old, Realm newRealm)
    {
        if (old != null)
        {
            old.OnBlockChanged -= BlockChanged;
        }
        newRealm.OnBlockChanged += BlockChanged;
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
    ushort curBlock;
    private void OnInteract(Vector2Int pos, Wall interfacedBlock, BlockState state, IInventoryContainer userInventory)
    {
        if(curPos == pos)
        {
            Close();
            return;
        }
        Detach();
        curPos = pos;
        curBlock = interfacedBlock.Id;
        gameObject.SetActive(true);
        bool found = false;
        foreach (var display in displays.Select(d => d.display))
        {
            display.gameObject.SetActive(false);
            if (!found && display.TypeMatch().IsAssignableFrom(interfacedBlock.GetType()))
            {
                display.gameObject.SetActive(true);
                display.DisplayInventory(pos, interfacedBlock, state, userInventory);
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

    private void BlockChanged(Chunk _, Vector2Int BlockPos, Vector2Int __, NativeBlockSlice block, BlockItemStack ___)
    {
        if(BlockPos == curPos && curBlock != block.wallBlock)
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

    public void Close()
    {
        gameObject.SetActive(false);
        curPos = null;
        curBlock = 0;
        Detach();
    }
}
