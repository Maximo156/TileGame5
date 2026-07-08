using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class VisibleTransformTarget : ITarget
{
    static LayerMask TerrainLayerMask = LayerMask.GetMask("Terrain");

    Transform agentTransform;
    float viewDist;

    Transform targetTransform;

    bool TransformValid = true;

    Vector3 LastValidPosition;

    public Vector3 Position
    {
        get
        {
            if (targetTransform == null)
                return Vector3.zero;

            if (!TransformValid)
                return LastValidPosition;

            return targetTransform.position;
        }
    }

    public VisibleTransformTarget(Transform targetTransform, Transform agentTransform, float viewDist)
    {
        this.agentTransform = agentTransform;
        this.viewDist = viewDist;
        this.targetTransform = targetTransform;
    }

    public bool IsValid()
    {
        var dir = targetTransform.position - agentTransform.position;
        TransformValid = Vector3.Distance(targetTransform.position, agentTransform.position) < viewDist
            && !Physics2D.Raycast(agentTransform.position, dir, dir.magnitude, TerrainLayerMask);

        if(TransformValid)
        {
            LastValidPosition = targetTransform.position;
        }

        return TransformValid || (LastValidPosition - agentTransform.position).sqrMagnitude > 1;
    }
}
