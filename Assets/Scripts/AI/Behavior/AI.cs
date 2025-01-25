using UnityEngine;

public class AI : MonoBehaviour, IAI
{
    public JobNavigator navigator;
    public ManualUpdateStateMachine stateMachine;

    public Transform Transform => transform;

    public IPathFinder pathfinder => navigator;

    public IBehavior behavior => stateMachine;

    public IAI ai => this;

    private void Start()
    {
        ai.Register();
    }

    public void Update()
    {
        navigator.Move(Time.deltaTime);
    }
}
