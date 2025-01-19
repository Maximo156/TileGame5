using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

[BurstCompile]
public struct AstarJob : IJob
{
    struct Node : IEquatable<Node>
    {
        public int2 ParentPos;
        public int2 Pos;

        public float DistanceToTarget;
        public float Cost;
        public float Weight;
        public bool IsDoor;
        public bool Walkable;

        public float F
        {
            get
            {
                if (DistanceToTarget != -1 && Walkable)
                    return (DistanceToTarget + Cost) / Weight;
                else
                    return int.MaxValue;
            }
        }
        public float2 Center
        {
            get
            {
                return new float2(Pos.x + 0.5f, Pos.y + 0.5f);
            }
        }

        public Node(int2 pos, bool walkable, bool isDoor, float weight = 1)
        {
            ParentPos = new int2();
            Pos = pos;
            DistanceToTarget = -1;
            Cost = 1;
            Weight = weight;
            Walkable = walkable;
            IsDoor = isDoor;
        }

        public override int GetHashCode()
        {
            return Pos.GetHashCode();
        }

        public bool Equals(Node other)
        {
            return Pos.Equals(other.Pos);
        }
    }

    [NativeDisableParallelForRestriction]
    [ReadOnly]
    public NativeHashMap<int2, BlockSliceData> BlockData;

    public int2 Start;
    public int2 End;
    public bool canUseDoors;
    public int MaxDistance;

    [WriteOnly]
    public NativeStack<int2> Path;

    [BurstCompile]
    public void Execute()
    {
        Node start = GetNode(Start);
        Node end = GetNode(End);

        NativeHashSet<Node> OpenList = new NativeHashSet<Node>(100, Allocator.Temp);
        NativeHashMap<int2, Node> ClosedList = new NativeHashMap<int2, Node>(100, Allocator.Temp);

        NativeList<Node> adjacencies = new NativeList<Node>(8, Allocator.Temp);
        OpenList.Add(start);
        while (OpenList.Count() > 0)
        {
            Node current = RemoveAndReturnBest(ref OpenList);
            ClosedList.Add(current.Pos, current);

            GetAdjacentNodes(current, ref adjacencies);

            bool found = false;
            for(int i = 0; i< adjacencies.Length; i++)
            {
                Node n = adjacencies[i];
                if (n.Equals(end))
                {
                    n.ParentPos = current.Pos;
                    if (n.Walkable)
                        ClosedList.TryAdd(n.Pos, n);
                    found = true;
                    break;
                }
                float startDistance = math.pow(CalcDistance(n.Pos, Start), 0.5f);
                float distance = CalcDistance(n.Pos, end.Pos);
                if (!ClosedList.ContainsKey(n.Pos) && n.Walkable && (canUseDoors || !n.IsDoor) && !OpenList.Contains(n) && startDistance < MaxDistance)
                {
                    n.ParentPos = current.Pos;
                    n.DistanceToTarget = distance;
                    n.Cost = 1 + current.Cost;
                    OpenList.Add(n);
                }
            }
            if (found) break;
        }

        if (ClosedList.ContainsKey(end.Pos))
        {
            var temp = ClosedList[end.Pos];
            do
            {
                Path.Push(temp.Pos);
                if(!ClosedList.TryGetValue(temp.ParentPos, out temp))
                {
                    break;
                }
            } while (!temp.Equals(start));
        }

        OpenList.Dispose();
        ClosedList.Dispose();
        adjacencies.Dispose();
    }

    [BurstCompile]
    float CalcDistance(int2 a, int2 b)
    {
        return math.pow(math.abs(a.x - b.x), 2) + math.pow(math.abs(a.y - b.y), 2);
    }

    [BurstCompile]
    Node RemoveAndReturnBest(ref NativeHashSet<Node> OpenList)
    {
        Node Min = new Node();
        foreach(var node in OpenList)
        {
            if(node.F <= Min.F)
            {
                Min = node;
            }
        }
        OpenList.Remove(Min);
        return Min;
    }

    [BurstCompile]
    void GetAdjacentNodes(Node n, ref NativeList<Node> temp)
    {
        temp.Clear();

        var up = new int2(1, 0);
        var down = new int2(-1, 0);
        var l = new int2(0, -1);
        var r = new int2(0, 1);

        var top = GetNode(n.Pos + up);
        var bottom = GetNode(n.Pos + down);
        var left = GetNode(n.Pos + l);
        var right = GetNode(n.Pos + r);


        temp.Add(top);
        temp.Add(bottom);
        temp.Add(left);
        temp.Add(right);

        if (top.Walkable && right.Walkable)
            temp.Add(GetNode(n.Pos + up + r));
        if (top.Walkable && left.Walkable)
            temp.Add(GetNode(n.Pos + up + l));
        if (bottom.Walkable && right.Walkable)
            temp.Add(GetNode(n.Pos + down + r));
        if (bottom.Walkable && left.Walkable)
            temp.Add(GetNode(n.Pos + down + l));
    }

    [BurstCompile]
    Node GetNode(int2 pos)
    {
        Node node;
        if (BlockData.TryGetValue(pos, out var info))
        {
            node = new Node(pos, info.Walkable, info.Door, math.max(info.MovementSpeed, 0.01f));
        }
        else
        {
            node = new Node(pos, false, false, 0.01f);
        }
        return node;
    }
}
