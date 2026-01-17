using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningBehavior : RangedUseBehavior, IStatefulItemBehaviour
{
    public int BlockDamage;
    protected override (bool used, bool useDurability) UseRanged(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        useInfo.stack.GetState<MiningBehaviorState>(out var miningState);
        var targetBlock = Vector2Int.FloorToInt(targetPosition.ToVector2());
        var info = BreakInfo.GetInfo();
        var roof = miningState?.mineRoof ?? false;
        if (info.dirty)
        {
            info.StartBreak(targetBlock, roof);
        }
        if (info.Hit(targetBlock, roof, BlockDamage))
        {
            ChunkManager.BreakBlock(targetBlock, roof);
            return (true, true);
        }
        return (true, false);
    }

    public ItemBehaviourState GetNewState()
    {
        return new MiningBehaviorState();
    }

    private class BreakInfo
    {
        static BreakInfo singleton;
        public bool dirty = true;
        ushort block;
        int hitsToBreak;
        int hits;

        BreakInfo()
        {
            PlayerMouseInput.OnAttackInterupted += SetDirty;
        }

        public void StartBreak(Vector2Int pos, bool roof)
        {
            if (ChunkManager.TryGetBlock(pos, out var blockSlice))
            {
                block = roof ? blockSlice.roofBlock : (blockSlice.wallBlock != 0 ? blockSlice.wallBlock : blockSlice.groundBlock);
                if (block != 0)
                {
                    dirty = false;
                    hits = 0;
                    hitsToBreak = BlockDataRepo.GetNativeBlock(block).hitsToBreak;
                }
            }
        }

        public bool Hit(Vector2Int pos, bool roof, int Damage)
        {
            if (!ChunkManager.TryGetBlock(pos, out var _) ||
                dirty ||
                block == 0)
            {
                SetDirty();
                return false;
            }
            hits += Damage;
            if (hits >= hitsToBreak)
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

public class MiningBehaviorState : ItemBehaviourState, IStateStringProvider, ICyclable
{
    public bool mineRoof;

    public void Cycle()
    {
        mineRoof = !mineRoof;
        TriggerStateChange();
    }

    public string GetStateString(Item _)
    {
        return "Mode: " + (mineRoof ? "Roof" : "Regular");
    }
}


