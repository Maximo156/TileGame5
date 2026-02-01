using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct NativeComponentAnchor
{
    public AnchorDirection direction;
    public int2 offset;
    public int key;
    public bool Lock;
}
