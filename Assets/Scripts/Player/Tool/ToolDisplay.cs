using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Linq;

public class ToolDisplay : MonoBehaviour
{
    public float a;
    public float b;

    [Serializable]
    public class ItemDisplayInfo
    {
        public Item item;
        public float scale = 1;
        public float zRotation = 45;
        public string AnimationName = "BasicSwing";
        public float AnimationSpeed = 1;
    }

    public ItemDisplayInfo DefaultInfo;
    public List<ItemDisplayInfo> ItemDisplayInfos;

    public ItemUseGridControl gridControl;

    private Animator animator;
    private SpriteRenderer display;
    private Dictionary<Item, ItemDisplayInfo> ItemDisplayDict;
    private ItemDisplayInfo curSelected;
    private Camera mainCamera;
    private Vector2 ScreenPos;
    private PlayerInventories inventory;
    private UserInfo userInfo;
    public void Awake()
    {
        mainCamera = Camera.main;
        ItemDisplayDict = ItemDisplayInfos.ToDictionary(e => e.item, e => e);
        animator = GetComponent<Animator>();
        display = GetComponentInChildren<SpriteRenderer>();
        PlayerInventories.OnHotBarChanged += ChangeHand;
        gameObject.SetActive(false);

        inventory = GetComponentInParent<PlayerInventories>();

        userInfo = new UserInfo
        {
            Health = GetComponentInParent<Health>(),
            Hunger = GetComponentInParent<Hunger>(),
            transform = transform.parent
        };
    }

    public void MoveMouse(InputAction.CallbackContext value)
    {
        ScreenPos = value.ReadValue<Vector2>();
        var mouseWorldPosition = mainCamera.ScreenToWorldPoint(ScreenPos);
        mouseWorldPosition.z = 0;
        var normalDirFromParent = mouseWorldPosition - transform.parent.position;
        var degAngle = Vector3.Angle(Vector3.right, normalDirFromParent);
        if (degAngle == 90 || degAngle == 270) degAngle += 0.0001f;
        var radAngle = Mathf.Deg2Rad*degAngle;
        var parameterized = Mathf.Atan(Mathf.Tan(radAngle) * a / b);
        var mult = (degAngle > 90 && degAngle < 270 ? -1 : 1);
        var offset = new Vector3(mult * a * Mathf.Cos(parameterized), mult * b * Mathf.Sin(parameterized) * Mathf.Sign(normalDirFromParent.y));
        transform.localPosition = offset;
        transform.up = offset;

        transform.localScale = new Vector3(mult, 1, 1);
    }

    ItemStack curInHand;
    int curSlot;
    private void ChangeHand(ItemStack newInHand, int slot)
    {
        gridControl.Close();
        curInHand = newInHand;
        curSlot = slot;
        if (gameObject.activeInHierarchy)
        {
            animating = false;
            animator.Play("Null");
        }
        if(newInHand == null)
        {
            curSelected = null;
            gameObject.SetActive(false);
            return;
        }
        if(!ItemDisplayDict.TryGetValue(newInHand.Item, out curSelected))
        {
            curSelected = DefaultInfo;
        }
        gameObject.SetActive(true);
        display.sprite = newInHand.Item.Sprite;
        display.transform.localScale = curSelected.scale * Vector3.one;
        display.transform.localEulerAngles = Vector3.forward * curSelected.zRotation;
        animator.SetFloat("Speed", curSelected.AnimationSpeed);
    }

    public void OnRightClick(Vector3 ScreenPos)
    {
        if (curInHand is null) return;
        if (curInHand.Item is IGridSource source)
        {
            gridControl?.Display(ScreenPos, source, curInHand.State as IGridClickListener);
        }
        if(curInHand.State is ICyclable cycle)
        {
            cycle.Cycle();
        }
    }

    public bool animating { get; private set; }
    Action onAnimFinished;
    public void OnStartAttack(Action onAnimFinished)
    {

        gridControl?.Close();
        this.onAnimFinished = onAnimFinished;
        if (curSelected != null && !animating && gameObject.activeInHierarchy)
        {
            animating = true;
            animator.Play(curSelected.AnimationName);
        }
    }

    public void AnimationAction()
    {
        curInHand?.Item.Use(
            transform.parent.position, 
            mainCamera.ScreenToWorldPoint(ScreenPos),
            new Item.UseInfo() {
                state = curInHand.State,
                availableInventory = inventory,
                UsedFrom = inventory.HotbarInv,
                UsedIndex = curSlot,
                UserInfo = userInfo
            });
    }

    public void AnimationEnd()
    {
        animating = false;
        onAnimFinished?.Invoke();
    }

    public void OnEnable()
    {
        if(curInHand == null)
        {
            gameObject.SetActive(false);
        }
    }
}
