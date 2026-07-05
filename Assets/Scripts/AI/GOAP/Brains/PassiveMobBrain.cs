using Goap;

public class PassiveMobBrain : BaseMobBrain
{
    protected override void InitAgentType()
    {
        if (provider.AgentTypeBehaviour == null)
            provider.AgentType = goap.GetAgentType(nameof(PassiveMobTypeConfigFactory));
    }

    private void Start()
    {
        provider.RequestGoal<WanderGoal, FollowHeldItemGoal, ReduceFearGoal>();
    }
}
