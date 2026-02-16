using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using EntityStatistics;
using Newtonsoft.Json;

public interface IInventory
{
    public delegate void ItemChanged(Inventory inv);
    public event ItemChanged OnItemChanged;
    public bool AddItem(ItemStack ItemStack);

    public bool AddItemIndex(ItemStack ItemStack, int index);

    public ItemStack CheckSlot(int index);

    public IEnumerable<ItemStack> GetAllItems(bool returnEmpty = true);

    public Dictionary<Item, int> GetItemCounts();

    public void RemoveItem(ItemStack ItemStack);

    public ItemStack RemoveItemIndex(int index);

    public bool CanAddSlot(ItemStack toAdd, int index);
}

public class Inventory : IInventory, IInventoryContainer, IGridSource
{
    public event IInventory.ItemChanged OnItemChanged;

    protected int count;

    [JsonProperty]
    protected ItemStack[] inv;

    protected void SetItem(ItemStack stack, int index)
    {
        if(inv[index] != null)
        {
            inv[index].OnStateChange -= CheckValidity;
        }
        inv[index] = stack;
        if (inv[index] != null)
        {
            inv[index].OnStateChange += CheckValidity;
        }
        SafeItemChange();
    }

    private void CheckValidity()
    {
        inv = inv.Select(i => ItemIsValid(i) ? i : null).ToArray();
        SafeItemChange();
    }

    private bool ItemIsValid(ItemStack stack)
    {
        if (stack == null) return false;
        if (stack.GetState<DurabilityState>(out var durability) && durability.CurDurability <= 0) return false;
        return true;
    }

    [JsonIgnore]
    public int Count => count;

    public Inventory()
    { }

    public Inventory(int count, List<ItemStack> initialItems = null)
    {
        this.count = count;
        inv = new ItemStack[count];

        if(initialItems != null)
        {
            for (int i = 0; i < initialItems.Count; i++)
            {
                SetItem(new ItemStack(initialItems[i].Item, initialItems[i].Count), i);
            }
        }
    }

    public virtual void Init()
    {
        inv = new ItemStack[count];
    }

    public bool AddItem(ItemStack stack)
    {
        var locStack = stack;
        var stackable = inv.Where(s => s != null && s.Item == locStack.Item && s.Count < locStack.Item.MaxStackSize);

        foreach (var s in stackable)
        {
            s.Combine(stack);
        }
        int nullIndex = inv.ToList().IndexOf(null);
        if (stack.Count > 0 && nullIndex != -1)
        {
            SetItem(stack, nullIndex);
            return true;
        }

        SafeItemChange();

        return stack.Count == 0;
    }

    public void AddItems(List<ItemStack> items)
    {
        foreach (var item in items)
        {
            AddItem(new ItemStack(item.Item, item.Count));
        }
    }

    public bool AddItemIndex(ItemStack stack, int index)
    {
        if (!CanAddSlot(stack, index)) return false;
        if (inv[index] == null)
        {
            SetItem(stack, index);
            return true;
        }
        else
        {
            if (inv[index].Item != stack.Item || inv[index].Item.MaxStackSize == inv[index].Count) return false;
            inv[index].Combine(stack);
            SafeItemChange();
            return stack.Count == 0;
        }
    }

    public Dictionary<Item, int> GetItemCounts()
    {
        return Utilities.ConvertToItemCounts(inv);
    }

    public void RemoveItem(ItemStack stack)
    {
        var local = stack;
        var stackable = inv.Where(s => s?.Item == local.Item);
        var count = stackable.Sum(s => s.Space);

        foreach (var s in stackable)
        {
            int toRemove = Math.Min(s.Count, stack.Count);
            s.Count -= toRemove;
            stack.Count -= toRemove;
        }

        inv = inv.Select(s => s == null || s.Count == 0 ? null : s).ToArray();

        SafeItemChange();
    }

    public virtual ItemStack RemoveItemIndex(int index)
    {
        var stack = inv[index];
        SetItem(null, index);
        return stack;
    }

    public virtual void RemoveItemIndex(int index, int count)
    {
        inv[index].Count -= count;
        if(inv[index].Count <= 0)
        {
            SetItem(null, index);
        }
        else
        {
            SafeItemChange();
        }
    }

    public virtual IEnumerable<ItemStack> GetAllItems(bool returnEmpty = true)
    {
        if (returnEmpty)
        {
            return inv;
        }
        return inv.Where(i => i != null);
    }

    public ItemStack CheckSlot(int index)
    {
        if (index >= inv.Length) return null;
        return inv[index];
    }

    public virtual bool CanAddSlot(ItemStack toAdd, int index)
    {
        var inSlot = inv[index];
        return inSlot == null || (inSlot?.Item == toAdd?.Item && inSlot.Space > 0);
    }

    public virtual void CopyToInventory(Inventory Other)
    {
        for (int i = 0; i < inv.Length; i++)
        {
            Other.SetItem(inv[i], i);
        }
    }

    public virtual void TransferToInventory(Inventory Other)
    {
        for (int i = 0; i < inv.Length; i++)
        {
            var Item = inv[i];
            if (Item != null)
            {
                if (Other.AddItem(Item))
                {
                    SetItem(null, i);
                }
            }
        }
    }

    public void RemoveItems(IEnumerable<ItemStack> Items)
    {
        foreach (var Item in Items)
        {
            var local = new ItemStack(Item);
            RemoveItem(local);
        }
    }

    public IEnumerable<IGridItem> GetGridItems()
    {
        return GetAllItems();
    }

    protected virtual void SafeItemChange()
    {
        if (!lockSafeItemChange)
        {
            CallbackManager.AddCallback(() => {
                OnItemChanged?.Invoke(this);
                });
        }
    }

    bool lockSafeItemChange = false;

    public InventoryTransaction GetTransaction()
    {
        return new InventoryTransaction(this);
    }

    public bool HasItem()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IInventory> GetIndividualInventories()
    {
        return new IInventory[] { this };
    }

    public class InventoryTransaction : IDisposable
    {
        readonly Inventory inventory;

        public InventoryTransaction(Inventory inv)
        {
            inventory = inv;
            inventory.lockSafeItemChange = true;
        }

        public void Dispose()
        {
            inventory.lockSafeItemChange = false;
            inventory.SafeItemChange();
        }
    }
}

public class AccessoryInv : Inventory
{
    public enum SlotType
    {
        Head, 
        Body,
        Legs
    }

    public AccessoryInv()
    {
        inv = new ItemStack[Enum.GetNames(typeof(SlotType)).Count()];
    }

    public Dictionary<Item, Modifier> Modifiers;

    public override bool CanAddSlot(ItemStack toAdd, int index)
    {
        if (toAdd.GetBehaviour<AccessoryBehaviour>(out var accessory))
        {
            return ((int)accessory.slotType == index) && base.CanAddSlot(toAdd, index);
        }
        return false;
    }
}

public class LimitedInventory : Inventory
{
    Func<Item, ItemStack[], int, bool> isAllowed;

    public LimitedInventory(Func<Item, ItemStack[], int, bool> isAllowed, int size) : base(size)
    {
        this.isAllowed = isAllowed;
    }

    public override bool CanAddSlot(ItemStack toAdd, int index)
    {
        if (isAllowed(toAdd?.Item, inv.ToArray(), index))
        {
            return base.CanAddSlot(toAdd, index);
        }
        return false;
    }
}
