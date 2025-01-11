using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ItemStack : IGridItem, IGridSource
{
    public delegate void StateChange();
    public event StateChange OnStateChange;

    public Item Item;
    public int Count;

    private ItemState _state;
    public ItemState State { get => _state; 
        private set
        {
            if (_state != null)
            {
                _state.OnStateChange -= TriggerStateChange;
            }
            _state = value;
            if (_state != null)
            {
                _state.OnStateChange += TriggerStateChange;
            }
        }
    }

    private void TriggerStateChange()
    {
        OnStateChange?.Invoke();
    }

    public int Space => Item.MaxStackSize - Count;

    public ItemStack(Item item, int count)
    {
        Item = item;
        Count = count;
        State = item.GetItemState();
    }

    public ItemStack(ItemStack ItemStack)
    {
        Item = ItemStack.Item;
        Count = ItemStack.Count;
        State = ItemStack.State ?? Item.GetItemState();
    }

    public void Combine(ItemStack b)
    {
        var total = Count + b.Count;

        Count = Mathf.Min(Count + b.Count, Item.MaxStackSize);
        b.Count = total - Count;
    }

    public bool Split(int num, out ItemStack res)
    {
        if (num > Count)
        {
            res = null;
            return false;
        }
        Count -= num;
        res = new ItemStack(Item, num);
        return true;
    }

    public string GetDisplayName()
    {
        return Item.name.Replace("Block", "").Replace("Item", "").SplitCamelCase() + (Count > 1 ? "s" : "");
    }

    public Sprite GetSprite()
    {
        return Item.Sprite;
    }

    public string GetString()
    {
        return Count > 1 ? Count.ToString() : "";
    }

    public float? GetFullness()
    {
        if(State is IDurableState durableState && Item is DurableItem durableItem)
        {
            return durableState.Durability.CurDurability * 1f / durableItem.MaxDurability;
        }
        return null;
    }

    public (string, string) GetTooltipString()
    {
        var stateString = State?.GetStateString();
        var statsString = Item.GetStatsString();
        string StatsString = string.Join('\n', (new []{ statsString, stateString }).Where(s => !string.IsNullOrWhiteSpace(s)));
        return (Item.formatedName, StatsString);
    }

    public override string ToString()
    {
        return $"{Item?.name} x {Count}";
    }

    public Color GetColor() => Item.Color;

    public IEnumerable<IGridItem> GetGridItems() => (State as IGridSource)?.GetGridItems() ?? Enumerable.Empty<IGridItem>();
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item", order = 1)]
public class Item : ScriptableObject, ISpriteful, ISaveable
{
    public Sprite Sprite;
    public Color Color = Color.white;
    public int MaxStackSize;
    public int BurnTime = 0;

    public string formatedName => name.Replace("Block", "").Replace("Item", "").SplitCamelCase();
    public string Identifier { get; set; } 
    Sprite ISpriteful.Sprite => Sprite;

    public virtual string GetStatsString()
    {
        var stats = this.ReadStats();
        return string.Join('\n', stats.OrderBy(kvp => kvp.Key)
                                  .Where(kvp => kvp.Value != null)
                                  .Select(s => s.Key.ToString().SplitCamelCase() + ": " + s.Value));
    }

    public virtual void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        
    }

    private void OnValidate()
    {
        Identifier = name;
    }

    public virtual ItemState GetItemState()
    {
        return null;
    }
}

public abstract class ItemState
{
    public delegate void ItemStateChanged();
    public event ItemStateChanged OnStateChange;

    public void TriggerStateChange()
    {
        OnStateChange?.Invoke();
    }

    public virtual string GetStateString()
    {
        return "";
    }
}

public interface ICyclable {
    public void Cycle();
}

public interface IColliderListener
{
    public void OnCollision(CollisionInfo info);
}

public struct CollisionInfo
{
    public ItemState state;
}

public struct UseInfo
{
    public ItemState state;
    public IInventoryContainer availableInventory;
    public Inventory UsedFrom;
    public int UsedIndex;
    public UserInfo UserInfo;
    public Collider2D ignoreCollider;
}
