using NativeRealm;
using UnityEngine;

namespace ComposableBlocks
{
    public class BedBehaviour : BlockBehaviour, IInteractableBehaviour
    {
        public bool Interact(ref NativeBlockSlice slice, InteractionWorldInfo worldInfo, InteractorInfo interactor)
        {
            var worldPos = worldInfo.WorldPos;
            var respawner = interactor.Respawner;
            if (respawner != null)
            {
                if (ChunkManager.CurRealm.AllowSetSpawn)
                {
                    respawner.SpawnPoint = worldPos;
                    respawner.SpawnRealm = ChunkManager.CurRealm.name;
                }
                if (DayTime.dayTime.IsNight)
                {
                    respawner.transform.position = Utilities.GetBlockCenter(worldPos).ToVector3();
                    respawner.Sleep();
                }
            }
            return true;
        }
    }
}
