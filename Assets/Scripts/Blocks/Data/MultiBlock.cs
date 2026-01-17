using NativeRealm;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "NewMultiBlock", menuName = "Block/MultiBlock", order = 1)]
public class MultiBlock : Wall, IConditionalPlace, IOnPlace
{
    public Vector2Int Dimensions;

    public bool CanPlace(Vector2Int Pos, Vector2Int dir)
    {
        var bounds = GetBounds(Pos, dir);
        foreach(var pos in bounds.allPositionsWithin)
        {
            if(!ChunkManager.TryGetBlock(pos.ToVector2Int(), out var slice) || slice.wallBlock != 0)
            {
                return false;
            }
        }
        return true;
    }

    public void OnPlace(Vector2Int Pos, Vector2Int dir, ref NativeBlockSlice slice)
    {
        var bounds = GetBounds(Pos, dir);
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (pos != Pos.ToVector3Int())
            {
                var state = (Pos - pos.ToVector2Int()).ToOffsetStateEncoding();
                ChunkManager.PlaceBlock(pos.ToVector2Int(), dir, ProxyBlock.Instance, false, state);
            }
            else
            {
                slice.simpleBlockState = (byte)System.Array.IndexOf(Utilities.QuadAdjacent, dir);
            }
        }
    }

    public override bool OnBreak(Vector2Int worldPos, BreakInfo info)
    {
        base.OnBreak(worldPos, info);
        var bounds = GetBounds(worldPos, Utilities.QuadAdjacent[info.slice.simpleBlockState]);
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (pos != worldPos.ToVector3Int())
            {
                ChunkManager.BreakBlock(pos.ToVector2Int(), false, false, false);
            }
        }
        return true;
    }

    BoundsInt GetBounds(Vector2Int Pos, Vector2Int dir)
    {
        if(dir.y != 0)
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
