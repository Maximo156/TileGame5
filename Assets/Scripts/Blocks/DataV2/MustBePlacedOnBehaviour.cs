using NativeRealm;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposableBlocks 
{
    public class MustBePlacedOnBehaviour : BlockBehaviour, IConditionalPlaceBehaviour
    {
        public List<Block> MustBePlacedOn;
        public bool CanPlace(Vector2Int Pos, Vector2Int dir, NativeBlockSlice slice)
        {
            return MustBePlacedOn.Any(b => b.Id == slice.groundBlock);
        }
    }
}
