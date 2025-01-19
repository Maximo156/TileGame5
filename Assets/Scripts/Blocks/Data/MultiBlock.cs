using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMultiBlock", menuName = "Block/MultiBlock", order = 1)]
public class MultiBlock : Wall, IConditionalPlace, IOnPlace
{
    public Vector2Int Dimensions;

    public bool CanPlace(Vector2Int Pos, Vector2Int dir)
    {
        var bounds = GetBounds(Pos, dir);
        foreach(var pos in bounds.allPositionsWithin)
        {
            if(!ChunkManager.TryGetBlock(pos.ToVector2Int(), out var slice) || slice.WallBlock is not null)
            {
                return false;
            }
        }
        return true;
    }

    public void OnPlace(Vector2Int Pos, Vector2Int dir)
    {
        var bounds = GetBounds(Pos, dir);
        foreach (var pos in bounds.allPositionsWithin)
        {
            ChunkManager.TryGetBlock(pos.ToVector2Int(), out var slice);
            if (pos != Pos.ToVector3Int())
            {
                slice.SetBlock(ProxyBlock.Instance);
                (slice.State as ProxyState).ActualPos = Pos;
            }
            else
            {
                (slice.State as MultiBlockState).Dir = dir;
            }
        }
    }

    public override bool OnBreak(Vector2Int worldPos, BreakInfo info)
    {
        var bounds = GetBounds(worldPos, (info.state as MultiBlockState).Dir);
        foreach (var pos in bounds.allPositionsWithin)
        {
            ChunkManager.TryGetBlock(pos.ToVector2Int(), out var slice, false);
            if (slice.State is ProxyState)
            {
                slice.Break(pos.ToVector2Int(), false, out var _, true);
            }
        }
        return true;
    }

    public override BlockState GetState()
    {
        return new MultiBlockState();
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

public class MultiBlockState : BlockState
{
    public Vector2Int Dir;

    public override void CleanUp(Vector2Int pos)
    {
    }
}
