using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleTransformResolver : TransformResolver
{
    public Transform toFollow;
    public override Transform GetTransform(float viewDistance) => Vector3.Distance(transform.position, toFollow.position) < viewDistance ? toFollow : null;
}
