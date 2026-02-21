using System.Collections.Generic;
using UnityEngine;

namespace ComposableBlocks
{
    public class CreaftingBlockBehaviour: BlockBehaviour, IInterfaceBlockBehaviour, IGridSource
    {
        public List<ItemRecipe> Recipes;

        public IEnumerable<IGridItem> GetGridItems() => Recipes;
    }
}
