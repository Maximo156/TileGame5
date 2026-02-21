using NativeRealm;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ComposableBlocks
{
    public class PortalBlockBehaviour : BlockBehaviour, IInteractableBehaviour
    {
        public delegate void PortalBlockUsed(string newDim, PortalBlock exitBlock, Vector2Int worldPos);
        public static event PortalBlockUsed OnPortalBlockUsed;

        [Header("Portal Info")]
        public string NewDim;
        public PortalBlock Exit;

        public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice, InteractorInfo interactor)
        {
            OnPortalBlockUsed?.Invoke(NewDim, Exit, worldPos);
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
