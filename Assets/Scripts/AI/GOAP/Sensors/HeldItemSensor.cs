using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using EntityStatistics;
using UnityEngine;

namespace Goap
{
    public class HeldItemSensor : MultiSensorBase
    {
        PlayerInventories playerInv;
        public HeldItemSensor()
        {
            playerInv = Object.FindAnyObjectByType<PlayerInventories>();

            AddLocalWorldSensor<IsItemHeld>((agent, references) =>
            {
                var interestingItems = references.GetCachedComponent<ItemInterestsBehavior>();
                if (interestingItems != null && interestingItems.InterestingItems.Contains(playerInv.curInHandItem)) 
                {
                    return 1;
                }
                return false;
            });

            AddLocalTargetSensor<ClosestPlayerInventory>((agent, references, target) =>
            {
                if (Vector3.Distance(agent.Transform.position, playerInv.transform.position)
                        > references.GetCachedComponent<ItemInterestsBehavior>().SenseDist) return null;
                if (target != null)
                    return target;

                return new TransformTarget(playerInv.transform);
            });
        }

        public override void Created()
        {}

        public override void Update()
        {}
    }
}
