using System;

[Serializable]
public class DurabilityBehaviour : StatefulItemBehaviour
{
    public float MaxDurability = 100;

    public override ItemBehaviourState GetNewState()
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
