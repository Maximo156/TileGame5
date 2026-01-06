using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntityStatistics;
using System.Linq;

[CreateAssetMenu(fileName = "NewAccessory", menuName = "Inventory/Accessory", order = 1)]
public class Accessory : Item
{
    public AccessoryInv.SlotType slotType;

    public List<BasicModifier.Info> Modifiers;

    public IEnumerable<Modifier> GenerateModifiers()
    {
        return Modifiers.Select(m => new BasicModifier(m));
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
