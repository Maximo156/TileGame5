using AOT;
using Unity.Burst;
using UnityEngine;

namespace ComposableBlocks
{
    public class ProxyBlock : Wall, INeedsStateFixup
    {
        public FunctionPointer<StructureGenerator.FixupSimpleState> GetFixupFunction()
        {
            return BurstCompiler.CompileFunctionPointer<StructureGenerator.FixupSimpleState>(FixState);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(StructureGenerator.FixupSimpleState))]
        public static byte FixState(ref StructureGenerator.Rotation rot, byte state)
        {
            var vec = state.ToOffsetState();
            vec.x *= rot.invX ? -1 : 1;
            vec.y *= rot.invY ? -1 : 1;
            vec = !rot.mirror ? vec : new Vector2Int(vec.y, vec.x);
            return vec.ToOffsetStateEncoding();
        }
    }
}
