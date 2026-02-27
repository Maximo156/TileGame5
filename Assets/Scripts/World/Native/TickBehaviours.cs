using UnityEngine;

public enum TickBehaviour
{
    None,
    Replace
}

public struct ReplaceBehaviourConfig
{
    public int MeanSecondsToHappen;
    public ushort nextBlock;
}

public struct TickInfo
{
    public TickBehaviour behaviour;
    public ReplaceBehaviourConfig replaceConfig;
}