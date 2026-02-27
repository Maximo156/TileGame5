using System.Collections.Generic;
using UnityEngine;

namespace ComposableBlocks
{
    public class InfusionBlockBehaviour: BlockBehaviour, IInterfaceBlockBehaviour
    {
        public List<ItemCharm> AllowedCharms;
    }
}
