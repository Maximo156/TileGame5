using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntityStatistics;
using System.Linq;

[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Inventory/ConsumableItem", order = 1)]
public class Consumable : Item
{
    [Header("Consumable Stats")]
    [Stat("Health")]
    public int Healing;

    [Stat("Mana")]
    public int ManaRestoration;

    [Stat("Food")]
    public int HungerRestoration;

    public List<BasicModifier.Info> Modifiers;

    public override void Use(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        useInfo.UserInfo.Health.ChangeStat(Healing);
        useInfo.UserInfo.Hunger.ChangeStat(HungerRestoration);
        useInfo.UserInfo.Stats.ApplyModifiers(Modifiers.Select(info => new BasicModifier(info)));
        useInfo.UsedFrom.RemoveItemIndex(useInfo.UsedIndex, 1);
    }

    public override string GetStatsString()
    {
        var baseString = base.GetStatsString();
        var statsString = string.Join('\n', Modifiers.OrderBy(info => info.StatType)
                                  .Select(info => info.ToString()));
        string StatsString = string.Join('\n', (new[] { baseString, statsString }).Where(s => !string.IsNullOrWhiteSpace(s)));
        return StatsString;
    }
}
