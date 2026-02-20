using NativeRealm;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBedBlock", menuName = "Block/BedBlock", order = 1)]
public class Bed : MultiBlock, IInteractableBlock
{
    public bool Interact(Vector2Int worldPos, ref NativeBlockSlice slice, InteractorInfo interactor)
    {
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