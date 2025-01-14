using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AStarSharp2
{
    public struct PathFindingResult
    {
        public Stack<Vector2Int> path;
        public HashSet<Node> Reachable;
        public bool FoundGoal;
        public bool canceled;
    }

    public interface IGrid
    {
        public Node GetNode(Vector2Int pos);

        public void Clear();
    }

    public class WorldGrid : IGrid
    {
        Dictionary<Vector2Int, Node> cache = new();
        public void Clear()
        {
            cache.Clear();
        }

        public Node GetNode(Vector2Int pos)
        {
            Node node;
            if(!cache.TryGetValue(pos, out node))
            {
                bool door = ChunkManager.TryGetBlock(pos, out var block) && block.WallBlock is Door;
                var weight = block?.MovementSpeed ?? 0;
                node = new Node(pos, block?.Walkable ?? false, door, weight);
                cache[pos] = node;
            }
            return node;
        }
    }

    public class Node
    {
        // Change this depending on what the desired size is for each element in the grid
        public static int NODE_SIZE = 1;
        public Node Parent;
        public Vector2Int Position;
        public Vector2 Center
        {
            get
            {
                return new Vector2(Position.x + NODE_SIZE / 2, Position.y + NODE_SIZE / 2);
            }
        }
        public float DistanceToTarget;
        public float Cost;
        public float Weight;
        public bool IsDoor;
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
        public bool Walkable;

        public Node(Vector2Int pos, bool walkable, bool isDoor, float weight = 1)
        {
            Parent = null;
            Position = pos;
            DistanceToTarget = -1;
            Cost = 1;
            Weight = weight;
            Walkable = walkable;
            IsDoor = isDoor;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var node = obj as Node;

            return node?.Position.Equals(Position) ?? false;
        }
    }

    public class ByF : IComparer<Node>
    {
        // Compares by Height, Length, and Width.
        public int Compare(Node x, Node y)
        {
            return x.F.CompareTo(y.F);
        }
    }

    public class Astar
    {
        public async Task<PathFindingResult> FindPathBiDirectional(Vector2Int Start, Vector2Int End, CancellationToken cancellationToken)
        {
            var forwards = Task.Run(() => FindPath(Start, End, null), cancellationToken);
            var backwards = Task.Run(() => FindPath(Start, End, null), cancellationToken);

            var res = await Task.WhenAny(forwards, backwards);
            var (path, accessible) = res.Result;
            if (path is not null && path.Peek() == End)
            {
                var tmp = new Stack<Vector2Int>();
                while (path.TryPop(out var next))
                {
                    tmp.Push(next);
                }
                path = tmp;
            }
            return new PathFindingResult
            {
                path = path,
                Reachable = accessible,
                FoundGoal = path != null,
                canceled = res.IsCanceled
            };
        }

        public static (Stack<Vector2Int> path, HashSet<Node> Accessible) FindPath(Vector2Int Start, Vector2Int End, IGrid Grid)
        {
            Grid ??= new WorldGrid();

            Node start = Grid.GetNode(Start);
            Node end = Grid.GetNode(End);

            Stack<Vector2Int> Path = new Stack<Vector2Int>();

            SortedSet<Node> OpenList = new SortedSet<Node>(new ByF());
            HashSet<Node> ClosedList = new HashSet<Node>();
            List<Node> adjacencies;
            Node current = start;

            // add start node to Open List
            OpenList.Add(start);

            while (OpenList.Count != 0 && !ClosedList.Contains(end))
            {
                current = OpenList.First();
                OpenList.Remove(current);

                ClosedList.Add(current);
                adjacencies = GetAdjacentNodes(current, Grid);

                foreach (Node n in adjacencies)
                {
                    if (!ClosedList.Contains(n) && n.Walkable && !OpenList.Contains(n))
                    {
                        n.Parent = current;
                        n.DistanceToTarget = Vector2Int.Distance(n.Position, end.Position);
                        n.Cost = n.Weight + n.Parent.Cost;
                        OpenList.Add(n);
                    }
                }
            }

            // construct path, if end was not closed return null
            if (!ClosedList.Contains(end)) return (null, ClosedList);

            // if all good, return path
            Node temp = current;
            do
            {
                Path.Push(temp.Position);
                temp = temp.Parent;
            } while (temp != start && temp != null);
            return (Path, null);
        }

        private static List<Node> GetAdjacentNodes(Node n, IGrid Grid)
        {
            List<Node> temp = new List<Node>();

            var top = Grid.GetNode(n.Position + Vector2Int.up);
            var bottom = Grid.GetNode(n.Position + Vector2Int.down);
            var left = Grid.GetNode(n.Position + Vector2Int.left);
            var right = Grid.GetNode(n.Position + Vector2Int.right);


            temp.Add(top);
            temp.Add(bottom);
            temp.Add(left);
            temp.Add(right);

            if (top.Walkable && right.Walkable)
                temp.Add(Grid.GetNode(n.Position + Vector2Int.up + Vector2Int.right));
            if (top.Walkable && left.Walkable)
                temp.Add(Grid.GetNode(n.Position + Vector2Int.up + Vector2Int.left));
            if (bottom.Walkable && right.Walkable)
                temp.Add(Grid.GetNode(n.Position + Vector2Int.down + Vector2Int.right));
            if (bottom.Walkable && left.Walkable)
                temp.Add(Grid.GetNode(n.Position + Vector2Int.down + Vector2Int.left));

            return temp;
        }
    }
}
