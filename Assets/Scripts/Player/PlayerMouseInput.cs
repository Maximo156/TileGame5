using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMouseInput : MonoBehaviour
{
    public delegate void AttackInterupted();
    public static event AttackInterupted OnAttackInterupted;

    public delegate void BlockInterfaced(Vector2Int pos, BlockSlice slice, IInventoryContainer inv);
    public static event BlockInterfaced OnBlockInterfaced;

    public delegate void InterfaceRangeExceeded();
    public static event InterfaceRangeExceeded OnInterfaceRangeExceeded;

    private Camera mainCamera;
    private Vector2 mouseWorldPosition;
    private Vector2Int mouseBlockPosition;
    private Vector2 screenPosition;
    private EventSystem events;

    [Header("Controls")]
    public ToolDisplay tool;
    public int InteractiveReach = 7;

    Inventory HotBarInv;
    PlayerInventories playerInventory;

    public void Awake()
    {
        mainCamera = Camera.main;
        events = FindFirstObjectByType<EventSystem>();
        playerInventory = GetComponent<PlayerInventories>();
        playerInventory.OnHotBarChanged += ChangeHand;
        HotBarInv = playerInventory.HotbarInv;
    }

    bool overGUI = false;
    public void Update()
    {
        overGUI = events.IsPointerOverGameObject();
        if (Keyboard.current.tKey.IsPressed())
        {
            var biomeInfo = ChunkManager.CurRealm.Generator.biomes.GetBiome(Vector2Int.FloorToInt(transform.position.ToVector2()));
            var worldInfo = ChunkManager.CurRealm.Generator.biomes.GetWorldInfo(Vector2Int.FloorToInt(transform.position.ToVector2()));

            print((biomeInfo != null ? biomeInfo.name : "Water") +" " + worldInfo);
        }
    }

    bool attackHeld = false;
    public void OnAttack(InputAction.CallbackContext value)
    {
        if (!overGUI && value.started)
        {
            attackHeld = true;
            PlayerAttack();
        }
        else if (value.canceled)
        {
            attackHeld = false;
            OnAttackInterupted?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext value)
    {
        if (!overGUI && value.started)
        {
            bool canInteract = Vector3.Distance(transform.position, mouseWorldPosition) <= InteractiveReach;
            if (Keyboard.current.shiftKey.isPressed && canInteract)
            {
                if(curInHand == null)
                {
                    var poppedItem = ChunkManager.PopItem(mouseBlockPosition);
                    if(poppedItem != null)
                    {
                        HotBarInv.AddItemIndex(poppedItem, handPosition);
                    }
                }
                else
                {
                    var inHand = HotBarInv.RemoveItemIndex(handPosition);
                    if(!ChunkManager.PlaceItem(mouseBlockPosition, inHand))
                    {
                        HotBarInv.AddItemIndex(inHand, handPosition);
                    }
                }
            }
            else if (!canInteract || !BlockInteract())
            {
                tool.OnRightClick(screenPosition);
            }
            else
            {
                tool.CloseDisplay();
            }
        }
    }

    public void MoveMouse(InputAction.CallbackContext value)
    {
        screenPosition = value.ReadValue<Vector2>();
        mouseWorldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        var block = Vector2Int.FloorToInt(mouseWorldPosition);
        if(block != mouseBlockPosition)
        {
            mouseBlockPosition = block;
            OnAttackInterupted?.Invoke();
        }
    }

    private void PlayerAttack()
    {
        if (attackHeld)
        {
            tool.OnStartAttack(PlayerAttack);
        }
    }

    ItemStack curInHand;
    int handPosition;
    private void ChangeHand(ItemStack newInHand, int pos)
    {
        curInHand = newInHand;
        handPosition = pos;
    }

    Vector2Int? CurInteractPos;
    private bool BlockInteract()
    {
        if (ChunkManager.Interact(mouseBlockPosition))
        {
            return true;
        }
        if(ChunkManager.TryGetBlock(mouseBlockPosition, out var slice) && slice.WallBlock is IInterfaceBlock)
        {
            CurInteractPos = CurInteractPos == mouseBlockPosition ? null : mouseBlockPosition;
            OnBlockInterfaced?.Invoke(mouseBlockPosition, slice, playerInventory);
            return true;
        }
        return false;
    }

    public void OnMove()
    {
        if(CurInteractPos is not null && Vector2.Distance(CurInteractPos.Value, PlayerMovement.PlayerTransform.position) > InteractiveReach)
        {
            CurInteractPos = null;
            OnInterfaceRangeExceeded?.Invoke();
        }
    }
}
