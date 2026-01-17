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
            if (ChunkManager.BreakBlock(targetBlock, roof) && useInfo.stack.GetState<DurabilityState>(out var durability))
            {
                return (true, true);
            }
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
                block = Utilities.GetActionableBlock(roof, blockSlice);
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
            if (hits >= block.HitsToBreak)
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


