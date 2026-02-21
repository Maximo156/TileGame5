using NativeRealm;
using UnityEngine;

namespace ComposableBlocks
{
    public class MultiBlockBehaviour : BlockBehaviour, IConditionalPlaceBehaviour, IOnPlaceBehaviour, IOnBreakBehaviour
    {
        public Vector2Int Dimensions;
        public bool CanPlace(Vector2Int Pos, Vector2Int dir)
        {
            var bounds = GetBounds(Pos, dir);
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!ChunkManager.TryGetBlock(pos.ToVector2Int(), out var slice) || slice.wallBlock != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void OnPlace(Vector2Int Pos, Vector2Int dir, ref NativeBlockSlice slice)
        {
            throw new System.NotImplementedException();
        }

        public void OnBreak(Vector2Int worldPos, BreakInfo info)
        {
            var bounds = GetBounds(worldPos, Utilities.QuadAdjacent[info.slice.simpleBlockState]);
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (pos != worldPos.ToVector3Int())
                {
                    ChunkManager.BreakBlock(pos.ToVector2Int(), false, false, false);
                }
            }
        }

        BoundsInt GetBounds(Vector2Int Pos, Vector2Int dir)
        {
            if (dir.y != 0)
            {
                var size = new Vector3Int(Dimensions.x, Dimensions.y, 1);
                var offset = dir.y > 0 ? Vector3Int.zero : size - Vector3Int.one;
                offset.z = 0;
                return new BoundsInt(Pos.ToVector3Int() - offset, size);
            }
            else
            {
                var size = new Vector3Int(Dimensions.y, Dimensions.x, 1);
                var offset = dir.x > 0 ? Vector3Int.zero : size - Vector3Int.one;
                offset.z = 0;
                return new BoundsInt(Pos.ToVector3Int() - offset, size);
            }
        }
    }
}
