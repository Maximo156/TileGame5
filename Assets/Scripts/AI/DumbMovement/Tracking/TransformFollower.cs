using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TransformFollower : SteeringBehavior
{
    TransformTracker Tracker;

    private void Awake()
    {
        Tracker = GetComponent<TransformTracker>();
    }

    protected override (float[] interests, float[] danger) GetWeightsImpl(Vector2[] Directions, float[] interests, float[] danger, ContextSteerer Steerer)
    {
        var (Pos, canSee) = Tracker.GetPosition();

        if(Pos == null)
        {
            return (interests, danger);
        }

        var dif = (Pos.Value - Steerer.transform.position).normalized;
        interests = Directions.Select((d, i) => Mathf.Max(interests[i], Vector2.Dot(dif, d))).ToArray();
        return (interests, danger);
    }
}
