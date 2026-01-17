using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using BlockDataRepos;
using NativeRealm;

public interface IConditionalPlace
{
    public bool CanPlace(Vector2Int Pos, Vector2Int dir);
}

public interface IOnPlace
{
    public void OnPlace(Vector2Int Pos, Vector2Int dir, ref NativeBlockSlice slice);
}

public class Block : ScriptableObject, ISpriteful
{
    ushort _id;
    public ushort Id { get
        {
            if (_id == 0) throw new System.Exception($"{Identifier} has no id");
            return _id;
        }
        set => _id = value;
    }
    public TileBase Display;
    public int HitsToBreak;
    public float MovementModifier = 0;
    public List<ItemStack> Drops;

    [SerializeField]
    private Sprite UiSprite;
    public Sprite Sprite => UiSprite;

    [SerializeField]
    private Color UiColor = Color.white;
    public Color Color => UiColor;
    string Identifier;

    private void OnValidate()
    {
        Identifier = name;
    }

    public virtual BlockState GetState()
    {
        return null;
    }

    public byte GetDefaultSimpleState()
    {
        return 0;
    }
     
    public virtual bool OnBreak(Vector2Int worldPos, BreakInfo info)
    {
        if (!info.dontDrop)
        {
            Utilities.DropItems(worldPos, Drops);
        }
        info.state?.CleanUp(worldPos);
        return true;
    }

    public string GetDisplayName()
    {
        return name.Replace("Block", "").Replace("Item", "").SplitCamelCase();
    }

    public struct BreakInfo
    {
        public BlockState state;
        public NativeBlockSlice slice;
        public bool dontDrop;
    }

    public virtual BlockData GetBlockData()
    {
        return new BlockData()
        {
            Level = GetLevel(),
            hitsToBreak = HitsToBreak,
        };
    }

    BlockLevel GetLevel()
    {
        if(this is Wall)
        {
            return BlockLevel.Wall;
        }
        else if(this is Ground)
        {
            return BlockLevel.Floor;
        }
        else
        {
            return BlockLevel.Roof;
        }
    }
}

public abstract class BlockState
{
    public abstract void CleanUp(Vector2Int pos);
}
