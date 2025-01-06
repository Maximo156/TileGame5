using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System;

public class RestrictedConcurentDictionary<TValue> : IDictionary<Vector2Int, TValue>
{
    public bool Dirty = true;
    protected IDictionary<Vector2Int, TValue> wrappedDictionary;
    private readonly int range;
    public RestrictedConcurentDictionary(int range)
    {
        wrappedDictionary = new ConcurrentDictionary<Vector2Int, TValue>();
        this.range = range;
    }

    void CheckRange(Vector2Int key)
    {
        if (key.x < 0 || key.x >= range || key.y < 0 || key.y >= range)
        {
            throw new InvalidOperationException($"{key} has component outside of range [0, {range})");
        }
    }

    public TValue this[Vector2Int key] 
    { 
        get => wrappedDictionary[key]; 
        set {
            CheckRange(key);
            wrappedDictionary[key] = value;
            Dirty = true;
        }
    }

    public ICollection<Vector2Int> Keys => wrappedDictionary.Keys;

    public ICollection<TValue> Values => wrappedDictionary.Values;

    public int Count => wrappedDictionary.Count;

    public bool IsReadOnly => wrappedDictionary.IsReadOnly;

    public void Add(Vector2Int key, TValue value)
    {
        CheckRange(key);
        wrappedDictionary.Add(key, value);
        Dirty = true;
    }

    public void Add(KeyValuePair<Vector2Int, TValue> item)
    {
        CheckRange(item.Key);
        wrappedDictionary.Add(item);
        Dirty = true;
    }

    public void Clear()
    {
        wrappedDictionary.Clear();
        Dirty = true;
    }

    public bool Contains(KeyValuePair<Vector2Int, TValue> item)
    {
        CheckRange(item.Key);
        return wrappedDictionary.Contains(item);
    }

    public bool ContainsKey(Vector2Int key)
    {
        CheckRange(key);
        return wrappedDictionary.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<Vector2Int, TValue>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerator<KeyValuePair<Vector2Int, TValue>> GetEnumerator()
    {
        return wrappedDictionary.GetEnumerator();
    }

    public bool Remove(Vector2Int key)
    {
        CheckRange(key);
        Dirty = true;
        return wrappedDictionary.Remove(key);
    }

    public bool Remove(KeyValuePair<Vector2Int, TValue> item)
    {
        CheckRange(item.Key);
        Dirty = true;
        return wrappedDictionary.Remove(item);
    }

    public bool TryGetValue(Vector2Int key, out TValue value)
    {
        CheckRange(key);
        return wrappedDictionary.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return wrappedDictionary.GetEnumerator();
    }
}
