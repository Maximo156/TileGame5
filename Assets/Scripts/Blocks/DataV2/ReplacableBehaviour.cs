namespace ComposableBlocks
{
    public class ReplacableBehaviour : BlockBehaviour, ISimpleTickBlockBehaviour
    {
        public Block nextBlock;
        public int MeanSecondsToHappen;

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
}
