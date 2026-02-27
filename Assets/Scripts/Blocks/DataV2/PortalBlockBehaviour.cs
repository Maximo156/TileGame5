using NativeRealm;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ComposableBlocks
{
    public class PortalBlockBehaviour : BlockBehaviour, IInteractableBehaviour
    {
        public delegate void PortalBlockUsed(string newDim, Block exitBlock, Vector2Int worldPos);
        public static event PortalBlockUsed OnPortalBlockUsed;

        [Header("Portal Info")]
        public string NewDim;

        public Block Exit;

        public bool Interact(ref NativeBlockSlice slice, InteractionWorldInfo worldInfo, InteractorInfo interactor)
        {
            OnPortalBlockUsed?.Invoke(NewDim, Exit, worldInfo.WorldPos);
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneUnloaded += ResetEvent;
        }

        static void ResetEvent(Scene _)
        {
            OnPortalBlockUsed = null;
        }
    }
}
