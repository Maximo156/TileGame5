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

    private Camera mainCamera;
    private Vector2 mouseWorldPosition;
    private Vector2Int mouseBlockPosition;
    private Vector2 screenPosition;
    private EventSystem events;

    [Header("Controls")]
    public ToolDisplay tool;

    Inventory HotBarInv;
    PlayerInventories playerInventory;

    public void Awake()
    {
        mainCamera = Camera.main;
        events = FindFirstObjectByType<EventSystem>();
        PlayerInventories.OnHotBarChanged += ChangeHand;
        playerInventory = GetComponent<PlayerInventories>();
        HotBarInv = playerInventory.HotbarInv;
    }

    bool overGUI = false;
    public void Update()
    {
        overGUI = events.IsPointerOverGameObject();
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
            if (Keyboard.current.shiftKey.isPressed)
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
            else if (!BlockInteract())
            {
                tool.OnRightClick(screenPosition);
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

    private bool BlockInteract()
    {
        if(!ChunkManager.Interact(mouseBlockPosition) && ChunkManager.TryGetBlock(mouseBlockPosition, out var slice) && slice.WallBlock is IInterfaceBlock)
        {
            OnBlockInterfaced?.Invoke(mouseBlockPosition, slice, playerInventory);
            return true;
        }
        return false;
    }
}
