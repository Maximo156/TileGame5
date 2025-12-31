using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SteeringBehavior: MonoBehaviour
{
    public bool showWieghts;
    protected Vector2[] cachedDirs;
    protected float[] cachedDanger = new float[0];
    protected float[] cachedinterests = new float[0];

    public (float[] interests, float[] danger) GetWeights(Vector2[] Directions, float[] interests, float[] danger, ContextSteerer Steerer)
    {
        cachedDirs = Directions;
        (cachedinterests, cachedDanger) = GetWeightsImpl(Directions, interests, danger, Steerer);
        return (cachedinterests, cachedDanger);
    }

    protected abstract (float[] interests, float[] danger) GetWeightsImpl(Vector2[] Directions, float[] interests, float[] danger, ContextSteerer Steerer);

    private void OnDrawGizmos()
    {
        if(showWieghts && Application.isPlaying && cachedDirs != null)
        {
            for (int i = 0; i < cachedDirs.Length; i++)
            {
                Gizmos.color = Color.yellow;
                var dir = cachedDirs[i] * Mathf.Abs(cachedinterests[i]);
                Gizmos.DrawRay(transform.position, dir);


                Gizmos.color = Color.red;
                dir = cachedDirs[i] * Mathf.Abs(cachedDanger[i]);
                Gizmos.DrawRay(transform.position, dir);
            }
        }
    }
}
