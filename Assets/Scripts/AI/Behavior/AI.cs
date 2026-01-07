using System;
using UnityEngine;

public class AI : MonoBehaviour, IAI
{
    public event Action<IAI> OnDespawn = delegate { };

    public JobNavigator navigator;
    public BaseBehavior baseBehavior;
    public bool m_Natural = true;
    public bool m_Hostile = false;
    public Transform Transform => transform;

    public IPathFinder pathfinder => navigator;

    public IBehavior behavior => baseBehavior;

    public IAI ai => this;

    public bool Natural => m_Natural;
    public bool Hostile => m_Hostile;

    private void Start()
    {
        ai.Register();
    }

    public void Update()
    {
        navigator.Move(Time.deltaTime);
    }

    public void OnDestroy()
    {
        OnDespawn(this);
    }
}
