using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TransformTracker : MonoBehaviour
{
    public float viewDistance;

    List<TransformResolver> resolvers;

    public Transform resolved { get; private set; }
    Vector3? LastSeen;

    bool reachedLastTarget = true;

    public void Awake()
    {
        resolvers = GetComponents<TransformResolver>().OrderBy(t => t.Priority).ToList();
    }

    public (Vector3?, bool) GetPosition()
    {
        if (reachedLastTarget)
        {
            resolved = resolvers.Select(r => r.GetTransform(viewDistance)).FirstOrDefault(t => t != null);
            reachedLastTarget = false;
        }

        bool canSee = false;
        if (resolved != null && CanSee(resolved.position))
        {
            canSee = true;
            LastSeen = resolved.position;
        }

        if (LastSeen is null || Vector2.Distance(transform.position, LastSeen.Value) < 0.5 || !CanSee(LastSeen.Value))
        {
            reachedLastTarget = true;
            resolved = null;
            LastSeen = null;
            return (null, false);
        }

        return (LastSeen, canSee);
    }

    public bool CanSee(Vector3 pos)
    {
        var dir = pos - transform.position;
        return Physics2D.Raycast(transform.position, dir, dir.magnitude, LayerMask.GetMask("Terrain")).collider == null;
    }
}
