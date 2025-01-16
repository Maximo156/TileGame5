using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading;

namespace AStarSharp
{
    public class PathFindingResult
    {
        public Stack<Vector2Int> path;
        public HashSet<Node> Reachable;
        public bool FoundGoal;
        public bool canceled = false;
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
        public static PathFindingResult FindPathNonCo(Vector2Int Start, Vector2Int End, Func<Node, List<Node>> GetAdjacent, bool canUseDoors, int MaxDistance = 30, CancellationToken cancelationToken = default)
        {
            Node start = GetNode(Start);
            Node end = GetNode(End);

            SortedSet<Node> OpenList = new SortedSet<Node>(new ByF());
            SortedSet<Node> OpenListBACK = new SortedSet<Node>(new ByF());

            HashSet<Vector2Int> Seen = new HashSet<Vector2Int>();
            HashSet<Vector2Int> SeenBACK = new HashSet<Vector2Int>();

            HashSet<Node> ClosedList = new HashSet<Node>();
            HashSet<Node> ClosedListBACK = new HashSet<Node>();
            List<Node> adjacencies;
            // add start node to Open List
            OpenList.Add(start);
            OpenListBACK.Add(end);
            while (OpenList.Any() && OpenListBACK.Any())
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    return new PathFindingResult()
                    {
                        FoundGoal = false,
                        Reachable = ClosedList,
                        canceled = true
                    };
                }
                
                // Forwards
                Node current = OpenList.First();

                OpenList.Remove(OpenList.First());
                ClosedList.Add(current);
                adjacencies = GetAdjacent(current);

                bool found = false;
                foreach (Node n in adjacencies)
                {
                    n.Weight = Mathf.Max(n.Weight, 0.01f);
                    if(n.Position == End)
                    {
                        n.Parent = current;
                        if (n.Walkable)
                            ClosedList.Add(n);
                        found = true;
                        break;
                    }
                    float startDistance = Vector2Int.Distance(Start, n.Position);
                    float distance = Mathf.Pow(MathF.Abs(End.x - n.Position.x), 2) + Mathf.Pow(MathF.Abs(End.y - n.Position.y), 2);
                    if (!ClosedList.Contains(n) && n.Walkable && (canUseDoors || !n.IsDoor) && !Seen.Contains(n.Position) && startDistance < MaxDistance)
                    {
                        n.Parent = current;
                        n.DistanceToTarget = distance;
                        n.Cost = 1 + n.Parent.Cost;
                        OpenList.Add(n);
                    }

                    Seen.Add(n.Position);
                }
                if (found) break;

                // Backwards
                Node currentBACK = OpenListBACK.First();
                OpenListBACK.Remove(OpenListBACK.First());
                ClosedListBACK.Add(currentBACK);
                List<Node> adjacenciesBACK = GetAdjacent(currentBACK);

                bool foundBACK = false;
                foreach (Node n in adjacenciesBACK)
                {
                    if (n.Position == Start)
                    {
                        n.Parent = currentBACK;
                        if(n.Walkable)
                            ClosedListBACK.Add(n);
                        foundBACK = true;
                        break;
                    }
                    float startDistance = Vector2Int.Distance(End, n.Position);
                    float distance = Mathf.Pow(MathF.Abs(Start.x - n.Position.x), 2) + Mathf.Pow(MathF.Abs(Start.y - n.Position.y), 2);

                    if (!ClosedListBACK.Contains(n) && n.Walkable && (canUseDoors || !n.IsDoor) && !SeenBACK.Contains(n.Position) && startDistance < MaxDistance)
                    {
                        n.Parent = currentBACK;
                        n.DistanceToTarget = distance;
                        n.Cost = 1 + n.Parent.Cost;
                        OpenListBACK.Add(n);
                    }

                    SeenBACK.Add(n.Position);
                }
                if (foundBACK) break;
            }
            Stack<Vector2Int> Result = new Stack<Vector2Int>();
            // if all good, return path
            Node tempForwards = ClosedList.MinBy(n => Vector2.Distance(n.Position, end.Position));
            Node tempBackwards = ClosedListBACK.MinBy(n => Vector2.Distance(n.Position, start.Position));
            
            if (tempForwards?.Position != End && tempBackwards?.Position != Start)
            {
                return new PathFindingResult()
                {
                    path = Result,
                    Reachable = ClosedList,
                    FoundGoal = false,
                };
            }

            var temp = tempForwards?.Position == End ? tempForwards : tempBackwards;
            do
            {
                try
                {
                    Result.Push(temp.Position);
                }
                catch (OutOfMemoryException)
                {
                    Debug.Log("Mem Limit Hit");
                    return null;
                }
                temp = temp.Parent;
            } while (temp != start && temp != null);

            if(tempForwards.Position != End)
            {
                var tmp = new Stack<Vector2Int>();
                while(Result.Count > 0)
                {
                    tmp.Push(Result.Pop());
                }
                Result = tmp;
            }

            return new PathFindingResult()
            {
                path = Result,
                Reachable = tempForwards.Position == End ? ClosedList : ClosedListBACK,
                FoundGoal = Result.Last() == End
            };
        }

        public static List<Node> GetAdjacentNodes(Node n)
        {
            List<Node> temp = new List<Node>();

            var top = GetNode(n.Position + Vector2Int.up);
            var bottom = GetNode(n.Position + Vector2Int.down);
            var left = GetNode(n.Position + Vector2Int.left);
            var right = GetNode(n.Position + Vector2Int.right);


            temp.Add(top);
            temp.Add(bottom);
            temp.Add(left);
            temp.Add(right);

            if (top.Walkable && right.Walkable)
                temp.Add(GetNode(n.Position + Vector2Int.up + Vector2Int.right));
            if (top.Walkable && left.Walkable)
                temp.Add(GetNode(n.Position + Vector2Int.up + Vector2Int.left));
            if (bottom.Walkable && right.Walkable)
                temp.Add(GetNode(n.Position + Vector2Int.down + Vector2Int.right));
            if (bottom.Walkable && left.Walkable)
                temp.Add(GetNode(n.Position + Vector2Int.down + Vector2Int.left));

            return temp;
        }

        public static Node GetNode(Vector2Int pos)
        {
            var weight = ChunkManager.GetMovementSpeed(pos);
            bool door = ChunkManager.TryGetBlock(pos, out var block) && weight != 0 && block.WallBlock is Door;
            Node node = new Node(pos, block?.Walkable ?? false, door, weight);
            return node;
        }
    }
}
