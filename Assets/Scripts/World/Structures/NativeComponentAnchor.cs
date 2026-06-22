using ComposableBlocks;
using Unity.Mathematics;

public struct NativeComponentAnchor
{
    public AnchorDirection direction;
    public int2 offset;
    public int key;
    public bool Lock;
}
