using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MobAvoider : SteeringBehavior
{
    public float Buffer;
    public float minDist;
    protected override (float[] interests, float[] danger) GetWeightsImpl(Vector2[] Directions, float[] interests, float[] danger, ContextSteerer Steerer)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(Steerer.transform.position, Buffer, LayerMask.GetMask("Player", "Mobs"));
        foreach (Collider2D obstacleCollider in colliders)
        {
            var directionToObstacle = obstacleCollider.transform.position - transform.position;
            float distanceToObstacle = directionToObstacle.magnitude;

            //calculate weight based on the distance Enemy<--->Obstacle
            float weight
                = distanceToObstacle <= minDist * 2
                ? 1.2f
                : (Buffer - distanceToObstacle) / Buffer;

            Vector2 directionToObstacleNormalized = directionToObstacle.normalized;

            danger = Directions.Select((dir, i) => Mathf.Max(danger[i], Vector2.Dot(directionToObstacleNormalized, dir) * weight)).ToArray();
        }

        return (interests, danger);
    }
}
