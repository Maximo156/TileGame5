using UnityEngine;

namespace ComposableBlocks
{
    [CreateAssetMenu(fileName = "Wall", menuName = "Blocks/Wall")]
    public class Wall : Block
    {
        /// <summary>
        /// Can support roofs
        /// </summary>
        public bool structural = false;

        /// <summary>
        /// Counts in enclosure calculations
        /// </summary>
        public bool solid = false;

        public bool walkable = false;
    }
}
