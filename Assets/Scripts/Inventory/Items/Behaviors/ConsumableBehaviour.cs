using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EntityStatistics;

public class ConsumableBehaviour : UseBehavior
{
    [Header("Consumable Stats")]
    [Stat("Health")]
    public int Healing;

    [Stat("Mana")]
    public int ManaRestoration;

    [Stat("Food")]
    public int HungerRestoration;

    public List<BasicModifier.Info> Modifiers;

    protected override(bool used, bool useDurability) UseImpl(Vector3 usePosition, Vector3 targetPosition, UseInfo useInfo)
    {
        useInfo.UserInfo.Health.ChangeStat(Healing);
        useInfo.UserInfo.Hunger.ChangeStat(HungerRestoration);
        useInfo.UserInfo.Stats.ApplyModifiers(Modifiers.Select(info => new BasicModifier(info)));
        useInfo.UsedFrom.RemoveItemIndex(useInfo.UsedIndex, 1);
        return (true, false);
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
