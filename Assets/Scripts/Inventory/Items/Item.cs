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

    public bool GetBehaviour<T, TState>(out T behaviour, out TState state) where T : class, IStatefulItemBehaviour where TState : ItemBehaviourState
    {
        behaviour = Item.Behaviors?.FirstOrDefault(b => b.GetType() == typeof(T)) as T;
        
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

    public bool GetBehaviour<T>(out T behaviour) where T : class
    {
        return Item.GetBehavior<T>(out behaviour);
    }

    public bool GetState<T>(out T state) where T : class
    {
        state = BehaviorStates.Values.FirstOrDefault(b => typeof(T).IsAssignableFrom(b.GetType())) as T;

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
        BehaviorStates = item.GetBehaviorStates();
    }

    public ItemStack(ItemStack ItemStack)
    {
        Item = ItemStack.Item;
        Count = ItemStack.Count;
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
        IEnumerable<string> strings;
        if(BehaviorStates != null)
        {
            strings = BehaviorStates.Values.Select(s => (s as IStateStringProvider)?.GetStateString(Item) ?? "").Append(Item.GetStatsString());
        }
        else
        {
            strings = new string[] { Item.GetStatsString() };
        }
        string StatsString = string.Join('\n',strings.Where(s => !string.IsNullOrWhiteSpace(s)));
        return (Item.formatedName, StatsString);
    }

    public override string ToString()
    {
        return $"{Item?.name} x {Count}";
    }

    public Color GetColor() => Item.Color;

    public IEnumerable<IGridItem> GetGridItems()
    {
        if(GetState<IGridSource>(out var source))
        {
            return source.GetGridItems();
        }
        return Enumerable.Empty<IGridItem>();
    }
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

    public bool GetBehavior<T>(out T behaviour) where T : class
    {
        behaviour = Behaviors.FirstOrDefault(b => typeof(T).IsAssignableFrom(b.GetType())) as T;

        return behaviour != null;
    }

    public virtual string GetStatsString()
    {
        return string.Join('\n', Behaviors.Select(b => b.GetStatsString()).Where(s => !string.IsNullOrEmpty(s)));
    }

    public void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
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

    public Dictionary<Type, ItemBehaviourState> GetBehaviorStates()
    {
        return Behaviors.Where(b => b is IStatefulItemBehaviour).ToDictionary(b => b.GetType(), b => (b as IStatefulItemBehaviour).GetNewState());
    }
}

public interface ICyclable {
    public void Cycle();
}

public struct CollisionInfo
{
    public ItemStack stack;
}

public struct UseInfo
{
    public ItemStack stack;
    public IInventoryContainer availableInventory;
    public Inventory UsedFrom;
    public int UsedIndex;
    public UserInfo UserInfo;
    public Collider2D ignoreCollider;
}
