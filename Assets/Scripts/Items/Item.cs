using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

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

    private Dictionary<Type, ItemBehaviourState> _behaviorStates;

    private Dictionary<Type, ItemBehaviourState> BehaviorStates
    {
        get => _behaviorStates;
        set
        {
            if (_behaviorStates != null)
            {
                foreach (var state in _behaviorStates.Values) { state.OnStateChange -= TriggerStateChange; }
            }
            _behaviorStates = value;
            if (_behaviorStates != null)
            {
                foreach (var state in _behaviorStates.Values) { state.OnStateChange += TriggerStateChange; }
            }
        }
    }

    public bool GetBehaviour<T, TState>(out T behaviour, out TState state) where T : StatefulItemBehaviour where TState : ItemBehaviourState
    {
        behaviour = (T)Item.Behaviors?.FirstOrDefault(b => b.GetType() == typeof(T));
        
        if (behaviour == null)
        {
            state = default;
            return false;
        }

        BehaviorStates.TryGetValue(typeof(T), out var _state);

        if(typeof(TState) != _state.GetType())
        {
            state = default;
            return false;
        }

        state = (TState)_state;
        return true;
    }

    public bool GetBehaviour<T>(out T behaviour) where T : ItemBehaviour
    {
        behaviour = (T)Item.Behaviors.FirstOrDefault(b => b.GetType() == typeof(T));

        return behaviour != null;
    }

    public bool GetState<T>(out T state) where T : ItemBehaviourState
    {
        state = (T)BehaviorStates.Values.FirstOrDefault(b => b.GetType() == typeof(T));

        return state != null;
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
        BehaviorStates = item.GetBehaviorStates();
    }

    public ItemStack(ItemStack ItemStack)
    {
        Item = ItemStack.Item;
        Count = ItemStack.Count;
        State = ItemStack.State ?? Item.GetItemState();
        BehaviorStates = ItemStack.BehaviorStates ?? Item.GetBehaviorStates();
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
        if(GetBehaviour<DurabilityBehaviour, DurabilityState>(out var behavior, out var state))
        {
            return state.CurDurability * 1f / behavior.MaxDurability;
        }
        return null;
    }

    public (string, string) GetTooltipString()
    {
        var stateString = State?.GetStateString(Item);
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

    [SerializeReference]
    public List<ItemBehaviour> Behaviors = new List<ItemBehaviour>();

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
        foreach(var useBehavior in Behaviors.Where(b => b is UseBehavior).Select(b => b as UseBehavior).OrderBy(b => b.priority))
        {
            if(useBehavior.Use(usePosition, targetPosition, useInfo))
            {
                break;
            }
        }
    }

    private void OnValidate()
    {
        Identifier = name;
    }

    public virtual ItemState GetItemState()
    {
        return null;
    }

    public Dictionary<Type, ItemBehaviourState> GetBehaviorStates()
    {
        return Behaviors.Where(b => b is StatefulItemBehaviour).ToDictionary(b => b.GetType(), b => (b as StatefulItemBehaviour).GetNewState());
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

    public virtual string GetStateString(Item item)
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
    public ItemStack stack;
}

public struct UseInfo
{
    public ItemState state;
    public ItemStack stack;
    public IInventoryContainer availableInventory;
    public Inventory UsedFrom;
    public int UsedIndex;
    public UserInfo UserInfo;
    public Collider2D ignoreCollider;
}
