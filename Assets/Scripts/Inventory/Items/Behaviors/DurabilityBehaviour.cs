using System;

[Serializable]
public class DurabilityBehaviour : ItemBehaviour, IStatefulItemBehaviour
{
    public float MaxDurability = 100;

    public ItemBehaviourState GetNewState()
    {
        return new DurabilityState(MaxDurability);
    }
}

public class DurabilityState : ItemBehaviourState
{
    public float CurDurability;

    public DurabilityState(float MaxDurability)
    {
        CurDurability = MaxDurability;
    }

    public void ChangeDurability(int dif)
    {
        CurDurability += dif;

        TriggerStateChange();
    }
}
