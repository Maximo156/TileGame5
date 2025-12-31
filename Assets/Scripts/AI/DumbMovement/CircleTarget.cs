using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleTarget : SteeringBehavior
{
    public float circleDistance;
    TransformTracker Tracker;

    private void Awake()
    {
        Tracker = GetComponent<TransformTracker>();
    }

    protected override (float[] interests, float[] danger) GetWeightsImpl(Vector2[] Directions, float[] interests, float[] danger, ContextSteerer Steerer)
    {
        var (pos, canSee) = Tracker.GetPosition();
        if (!canSee)
        {
            return (interests, danger);
        }

        //interests = Directions.Select

        return (interests, danger);
    }
}
