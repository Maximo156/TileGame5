using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using NativeRealm;
using BlockDataRepos;

[BurstCompile]
public struct AStarJob : IJob
{
    [ReadOnly] public RealmData realmData;
    [ReadOnly] public NativeBlockDataRepo blockInfo;

    public int2 Start;
    public int2 End;

    public bool canUseDoors;
    public int MaxDistance;
    public int ReachableRange;
    public int chunkWidth;

    public SearchBuffer Search;
    public NativeBinaryMinHeap Open;

    [WriteOnly] public NativeStack<float2> Path;
    [WriteOnly] public NativeList<int2> Reachable;

    static readonly int2[] Offsets =
    {
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
        new(1, 1),
        new(-1, 1),
        new(1, -1),
        new(-1, -1)
    };

    static readonly float2 DiagCost = new(1.41421356f, 1.41421356f);

    public void Execute()
    {
        Search.Reset(Start);
        Open.Clear();

        ushort startIndex = (ushort)Search.PositionToIndex(Start);

        ref var startNode = ref Search.Get(startIndex);
        startNode.G = 0;
        startNode.Parent = ushort.MaxValue;
        startNode.State = NodeState.Open;

        Open.Push(startIndex, Heuristic(Start, End));

        while (!Open.IsEmpty)
        {
            ushort currentIndex = Open.Pop();
            ref var current = ref Search.Get(currentIndex);

            if (current.State == NodeState.Closed)
                continue;

            current.State = NodeState.Closed;

            int2 currentPos = Search.IndexToPosition(currentIndex);

            if (currentPos.Equals(End))
            {
                Reconstruct(currentIndex);
                return;
            }

            if (Chebyshev(Start, currentPos) <= ReachableRange)
                Reachable.Add(currentPos);

            Expand(currentIndex, currentPos);
        }
    }

    void Expand(ushort currentIndex, int2 currentPos)
    {
        ref var current = ref Search.Get(currentIndex);

        float currentG = current.G;

        for (int i = 0; i < 8; i++)
        {
            int2 np = currentPos + Offsets[i];

            if (!Search.Contains(np))
                continue;

            bool diagonal = i >= 4;

            if (diagonal && !CanMoveDiagonal(currentPos, np))
                continue;

            if (!TryGetMovement(np, out var move))
                continue;

            if (!move.walkable)
                continue;

            if (move.door && !canUseDoors)
                continue;

            ushort nIndex = (ushort)Search.PositionToIndex(np);
            ref var node = ref Search.Get(nIndex);

            float stepCost = diagonal ? 1.41421356f : 1f;
            stepCost /= math.max(move.movementSpeed, 0.01f);

            float tentativeG = currentG + stepCost;

            if (node.State != NodeState.Unvisited && tentativeG >= node.G)
                continue;

            node.G = tentativeG;
            node.Parent = currentIndex;
            node.State = NodeState.Open;

            float f = tentativeG + Heuristic(np, End);

            Open.Push(nIndex, f);
        }
    }

    void Reconstruct(ushort endIndex)
    {
        ushort current = endIndex;

        while (current != ushort.MaxValue)
        {
            ref var node = ref Search.Get(current);

            int2 pos = Search.IndexToPosition(current);
            Path.Push(pos + new float2(0.5f, 0.5f));

            current = node.Parent;
        }
    }

    bool CanMoveDiagonal(int2 from, int2 to)
    {
        int2 d = to - from;

        int2 a = from + new int2(d.x, 0);
        int2 b = from + new int2(0, d.y);

        return IsWalkable(a) && IsWalkable(b);
    }

    bool IsWalkable(int2 pos)
    {
        return TryGetMovement(pos, out var m) && m.walkable;
    }

    bool TryGetMovement(int2 pos, out SliceMoveInfo move)
    {
        var (chunkPos, local) = GetChunkAndPos(pos);

        if (realmData.TryGetChunk(chunkPos, out var chunk))
        {
            var slice = chunk.GetSlice(local.x, local.y);
            move = slice.GetMovementInfo(blockInfo);
            return true;
        }

        move = default;
        return false;
    }

    static float Heuristic(int2 a, int2 b)
    {
        int dx = math.abs(a.x - b.x);
        int dy = math.abs(a.y - b.y);

        int min = math.min(dx, dy);
        int max = math.max(dx, dy);

        return 1.41421356f * min + (max - min);
    }

    static int Chebyshev(int2 a, int2 b)
        => math.max(math.abs(a.x - b.x), math.abs(a.y - b.y));

    (int2 chunk, int2 local) GetChunkAndPos(int2 pos)
    {
        var c = math.int2(math.floor((float2)pos / chunkWidth));
        return (c, pos - c * chunkWidth);
    }
}