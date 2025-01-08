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
    private PolygonCollider2D m_collider2D;

    public void Awake()
    {
        mainCamera = Camera.main;
        ItemDisplayDict = ItemDisplayInfos.ToDictionary(e => e.item, e => e);
        animator = GetComponent<Animator>();
        display = GetComponentInChildren<SpriteRenderer>();
        PlayerInventories.OnHotBarChanged += ChangeHand;
        gameObject.SetActive(false);

        inventory = GetComponentInParent<PlayerInventories>();
        m_collider2D = GetComponentInChildren<PolygonCollider2D>();
        m_collider2D.enabled = false;

        userInfo = new UserInfo
        {
            Health = GetComponentInParent<Health>(),
            Hunger = GetComponentInParent<Hunger>(),
            transform = transform.parent
        };
        Physics2D.IgnoreCollision(m_collider2D, m_collider2D);
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
        onAnimFinished = null;
        AnimationEnd();
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
        UpdateCollider();
        display.transform.localScale = curSelected.scale * Vector3.one;
        display.transform.localEulerAngles = Vector3.forward * curSelected.zRotation;
        animator.SetFloat("Speed", curSelected.AnimationSpeed);
    }


    bool useCollider => display?.sprite?.GetPhysicsShapeCount() > 0;
    void UpdateCollider()
    {
        var pointsList = new List<Vector2>();

        if (useCollider)
        {
            display.sprite.GetPhysicsShape(0, pointsList);
            m_collider2D.points = pointsList.ToArray();
        }
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
            m_collider2D.enabled = useCollider;
            animating = true;
            animator.Play(curSelected.AnimationName);
        }
    }

    public void AnimationAction()
    {
        curInHand?.Item.Use(
            transform.parent.position, 
            mainCamera.ScreenToWorldPoint(ScreenPos),
            new UseInfo() {
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
        itemDamaged = false;
        m_collider2D.enabled = false;
        onAnimFinished?.Invoke();
    }

    public void OnEnable()
    {
        if(curInHand == null)
        {
            gameObject.SetActive(false);
        }
    }

    bool itemDamaged = false;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!animating)
        {
            return;
        }

        if(!itemDamaged && curInHand.Item is IColliderListener listener)
        {
            listener.OnCollision(new CollisionInfo()
            {
                state = curInHand.State
            });
            itemDamaged = true;
        }
    }
}
