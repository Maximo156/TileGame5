using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Inventory/ConsumableItem", order = 1)]
public class Consumable : Item
{
    [Header("Consumable Stats")]
    [Stat("Healing")]
    public int Healing;

    [Stat("ManaRestoration")]
    public int ManaRestoration;

    [Stat("Food")]
    public int HungerRestoration;

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        useInfo.UserInfo.Health.ChangeStat(Healing);
        useInfo.UserInfo.Hunger.ChangeStat(HungerRestoration);
        useInfo.UsedFrom.RemoveItemIndex(useInfo.UsedIndex, 1);
    }
}
