using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewMiningTool", menuName = "Inventory/MiningTool", order = 1)]
public class MiningTool : Tool
{
    [Header("Mining Settings")]
    public int Damage;

    public override bool PerformAction(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        if (CanReach(usePosition, targetPosition))
        {
            var miningState = useInfo.state as MiningToolState;
            var targetBlock = Vector2Int.FloorToInt(targetPosition.ToVector2());
            var info = BreakInfo.GetInfo();
            var roof = miningState.mineRoof;
            if (info.dirty)
            {
                info.StartBreak(targetBlock, roof);
            }
            if (info.Hit(targetBlock, roof, Damage))
            {
                if (ChunkManager.BreakBlock(targetBlock, roof))
                {
                    miningState.Durability.ChangeDurability(-1);
                    return true;
                }
            }
        }
        return false;
    }

    public override ItemState GetItemState()
    {
        return new MiningToolState(this);
    }

    private class BreakInfo
    {
        static BreakInfo singleton;
        public bool dirty = true;
        Block block;
        int hits;

        BreakInfo()
        {
            PlayerMouseInput.OnAttackInterupted += SetDirty;
        }

        public void StartBreak(Vector2Int pos, bool roof)
        {
            if (ChunkManager.TryGetBlock(pos, out var blockSlice))
            {
                block = roof ? blockSlice.RoofBlock : (blockSlice.WallBlock as Block ?? blockSlice.GroundBlock);
                if (block != null)
                {
                    dirty = false;
                    hits = 0;
                }
            }
        }

        public bool Hit(Vector2Int pos, bool roof, int Damage)
        {
            if (!ChunkManager.TryGetBlock(pos, out var _) ||
                dirty || 
                block == null)
            {
                SetDirty();
                return false;
            }
            hits += Damage;
            if(hits >= block.HitsToBreak)
            {
                dirty = true;
                return true;
            }
            return false;
        }

        private void SetDirty()
        {
            dirty = true;
        }

        public static BreakInfo GetInfo()
        {
            return singleton ??= new BreakInfo();
        }
    }
}

public class MiningToolState : ItemState, IDurableState, ICyclable
{
    public bool mineRoof;
    public DurableState Durability { get; }

    public MiningToolState(MiningTool tool)
    {
        Durability = new(tool, this);
    }

    public void Cycle()
    {
        mineRoof = !mineRoof;
        TriggerStateChange();
    }

    public override string GetStateString()
    {
        return "Mode: " + (mineRoof ? "Roof" : "Regular");
    }
}
