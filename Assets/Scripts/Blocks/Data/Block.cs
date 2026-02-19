using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using BlockDataRepos;
using NativeRealm;
using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEditor;

public interface IConditionalPlace
{
    public bool CanPlace(Vector2Int Pos, Vector2Int dir);
}

public interface IOnPlace
{
    public void OnPlace(Vector2Int Pos, Vector2Int dir, ref NativeBlockSlice slice);
}

[JsonConverter(typeof(BlockConverter))]
public class Block : ScriptableObject, ISpriteful
{
    [ReadOnlyProperty]
    public ushort Id = 0;

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Id == 0)
        {
            FixupBlockId();
        }
    }

    private void OnEnable()
    {
        if (Id == 0)
        {
            FixupBlockId();
        }
    }

    void FixupBlockId()
    {
        var tmp = Resources.LoadAll<Block>("ScriptableObjects/Blocks").OrderBy(b => b.name).ToList();
        var ordered = tmp.OrderBy(b => b.Id).ToList();
        var set = new HashSet<ushort>(ordered.Select(b => b.Id));
        for(ushort i = 1; i <= ordered.Count; i++)
        {
            if (!set.Contains(i))
            {
                SetBlockId(this, i);
                return;
            }
        }
    }

    public static void SetBlockId(Block block, ushort id, bool save = true)
    {
        block.Id = id;
        EditorUtility.SetDirty(block);

        if (save)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
#endif
}

public abstract class BlockState
{
    public event Action OnStateUpdated;
    public abstract void CleanUp(Vector2Int pos);

    protected void TriggerStateChange()
    {
        OnStateUpdated?.Invoke();
    }
}
