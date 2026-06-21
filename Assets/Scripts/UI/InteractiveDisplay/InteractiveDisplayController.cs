using BlockDataRepos;
using ComposableBlocks;
using NativeRealm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class InteractiveDislay : MonoBehaviour
{
    public virtual bool OpenInv { get; } = true;
    public abstract Type TypeMatch();
    public abstract void InitDisplay(Vector2Int worldPos, Block interfacedBlock, BlockState state, byte simpleState, IInventoryContainer otherInventory);
    public abstract void Detach();

    public virtual void Reposition(Vector3 pos)
    {

    }
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

    public PlayerInventoryDisplay inventoryDisplay;

    InteractiveDislay activeDisplay;

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
            var pos = Cam.WorldToScreenPoint(curPos.Value.ToVector3Int());
            transform.position = pos;
            activeDisplay.Reposition(pos);
        }
    }

    Vector2Int? curPos;
    ushort curBlock;
    private void OnInteract(Vector2Int pos, Block interfacedBlock, BlockState state, byte simpleState, IInterfaceBlockBehaviour behaviour, IInventoryContainer userInventory)
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
            if (!found && display.TypeMatch().IsAssignableFrom(behaviour.GetType()))
            {
                display.gameObject.SetActive(true);
                display.InitDisplay(pos, interfacedBlock, state, simpleState, userInventory);
                if (display.OpenInv)
                {
                    inventoryDisplay.InventorySetActive(true);
                }
                found = true;
                activeDisplay = display;
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
        activeDisplay = null;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        curPos = null;
        curBlock = 0;
        Detach();
    }
}
