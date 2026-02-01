using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public struct NativeStructureComponent : IWeighted
{
    public int Importance;
    public bool AllowMountains;
    public FixedString32Bytes Name;

    public BoundsInt Bounds;
    public SliceData BlocksSlice;
    public SliceData AnchorSlice;

    public int Weight => Importance;
}
