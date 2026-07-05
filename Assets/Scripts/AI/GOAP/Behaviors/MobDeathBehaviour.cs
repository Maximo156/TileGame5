using CrashKonijn.Agent.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class MobDeathBehaviour : MonoBehaviour
{
    public List<ItemStack> Drops;
    private BaseMobBrain baseBrain;

    private void Awake()
    {
        baseBrain = GetComponent<BaseMobBrain>();
    }

    void Die()
    {
        baseBrain.PlayAnimation("Die", OnDeath, true);
    }

    void OnDeath()
    {
        Utilities.DropItems(Utilities.GetBlockPos(transform.position), Drops);
        Destroy(gameObject);
    }
}
