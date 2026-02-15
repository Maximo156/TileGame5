using BlockDataRepos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewReplacableWallBlock", menuName = "Block/ReplacableWall", order = 1)]
public class ReplacableBlock : Wall, ISimpleTickBlock
{
    public Wall nextBlock;
    public int MeanSecondsToHappen;

    protected virtual Wall NewBlock()
    {
        return nextBlock;
    }

    public TickInfo GetTickInfo()
    {
        return new TickInfo
        {
            behaviour = TickBehaviour.Replace,
            replaceConfig = new ReplaceBehaviourConfig()
            {
                MeanSecondsToHappen = MeanSecondsToHappen,
                nextBlock = nextBlock.Id
            }
        };
    }
}
