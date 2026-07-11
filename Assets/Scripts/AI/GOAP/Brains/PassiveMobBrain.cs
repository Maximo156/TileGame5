using Goap;

public class PassiveMobBrain : BaseMobBrain
{
    protected override void InitAgentType()
    {
        if (provider.AgentTypeBehaviour == null)
            provider.AgentType = goap.GetAgentType(nameof(PassiveMobTypeConfigFactory));
    }

    protected override void SetBaseGoals()
    {
        provider.RequestGoal<WanderGoal, FollowHeldItemGoal, ReduceFearGoal>();
    }
}
