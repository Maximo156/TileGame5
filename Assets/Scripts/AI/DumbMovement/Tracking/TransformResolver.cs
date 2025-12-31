using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TransformResolver : MonoBehaviour
{
    public int Priority;

    public abstract Transform GetTransform(float viewDistance);
}
