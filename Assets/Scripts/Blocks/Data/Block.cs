using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class Block : ScriptableObject, ISpriteful, ISaveable
{
    public TileBase Display;
    public int HitsToBreak;
    public List<ItemStack> Drops;

    [SerializeField]
    private Sprite UiSprite;
    public Sprite Sprite => UiSprite;

    [SerializeField]
    private Color UiColor = Color.white;
    public Color Color => UiColor;

    public string Identifier { get; set; }

    private void OnValidate()
    {
        Identifier = name;
    }

    public virtual BlockState GetState()
    {
        return null;
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
        public bool dontDrop;
    }
}

public abstract class BlockState
{
    public abstract void CleanUp(Vector2Int pos);
}
