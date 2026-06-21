using UnityEngine;

namespace ComposableBlocks
{
    public class AnchorBlockBehaviour : BlockBehaviour, ISimpleStateBlockBehaviour, IInterfaceBlockBehaviour
    {
        public byte GetState()
        {
            return 0;
        }

        public static byte GetState(bool key, int code, AnchorDirection dir)
        {
            if ((code & ~0b0001_1111) != 0)
                throw new System.Exception("invalid code");

            if (dir is < AnchorDirection.Up or > AnchorDirection.Left)
                throw new System.Exception("invalid direction");

            return (byte)(
                code |
                ((int)dir << 5) |
                (key ? 0b1000_0000 : 0)
            );
        }

        public static (int code, bool key, AnchorDirection dir) DecodeState(byte state)
        {
            return (
                state & 0b0001_1111,
                (state & 0b1000_0000) != 0,
                (AnchorDirection)((state >> 5) & 0b11)
            );
        }
    }
}
