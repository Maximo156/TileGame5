using UnityEngine;

public enum TickBehaviour
{
    None,
    Replace
}

public struct ReplaceBehaviourInfo
{
    public int MeanSecondsToHappen;
    public ushort nextBlock;
}