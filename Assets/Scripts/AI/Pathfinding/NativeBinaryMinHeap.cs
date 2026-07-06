using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct NativeBinaryMinHeap : IDisposable
{
    struct HeapNode
    {
        public ushort Index;
        public float Priority;
    }

    NativeList<HeapNode> _data;

    public bool IsCreated => _data.IsCreated;
    public bool IsEmpty => _data.Length == 0;
    public int Count => _data.Length;

    public NativeBinaryMinHeap(int capacity, Allocator allocator)
    {
        _data = new NativeList<HeapNode>(capacity, allocator);
    }

    public void Clear()
    {
        _data.Clear();
    }

    public void Dispose()
    {
        if (_data.IsCreated)
            _data.Dispose();
    }

    public JobHandle Dispose(JobHandle dep)
    {
        if (_data.IsCreated)
            return _data.Dispose(dep);
        return default;
    }

    public void Push(ushort index, float priority)
    {
        var node = new HeapNode { Index = index, Priority = priority };
        _data.Add(node);
        SiftUp(_data.Length - 1);
    }

    public ushort Pop()
    {
        var root = _data[0].Index;

        int last = _data.Length - 1;
        _data[0] = _data[last];
        _data.RemoveAt(last);

        if (_data.Length > 0)
            SiftDown(0);

        return root;
    }

    void SiftUp(int i)
    {
        while (i > 0)
        {
            int p = (i - 1) >> 1;

            if (_data[i].Priority >= _data[p].Priority)
                break;

            (_data[i], _data[p]) = (_data[p], _data[i]);
            i = p;
        }
    }

    void SiftDown(int i)
    {
        int count = _data.Length;

        while (true)
        {
            int l = (i << 1) + 1;
            if (l >= count) break;

            int r = l + 1;
            int m = (r < count && _data[r].Priority < _data[l].Priority) ? r : l;

            if (_data[i].Priority <= _data[m].Priority)
                break;

            (_data[i], _data[m]) = (_data[m], _data[i]);
            i = m;
        }
    }
}
