using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class RunBehavior : MonoBehaviour
{
    private GoapActionProvider provider;
    private GridNavAgent navAgent;

    private void Awake()
    {
        provider = GetComponent<GoapActionProvider>();
        navAgent = GetComponent<GridNavAgent>();
    }

    private void OnEnable()
    {
        provider.Events.OnGoalCompleted += OnGoalCompleted;
        provider.Events.OnGoalStart += OnGoalStart;
    }

    private void OnDisable()
    {
        provider.Events.OnGoalCompleted -= OnGoalCompleted;
        provider.Events.OnGoalStart -= OnGoalStart;
    }

    private void OnGoalCompleted(IGoal goal)
    {
        if (goal is IRunningGoal)
        {
            navAgent.StopRunning();
        }
    }

    private void OnGoalStart(IGoal goal)
    {
        if (goal is IRunningGoal)
        {
            navAgent.Run();
        }
    }
}
