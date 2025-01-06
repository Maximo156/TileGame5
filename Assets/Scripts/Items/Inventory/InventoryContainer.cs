using System.Linq;
using System.Collections.Generic;

public interface IInventoryContainer
{
    public ItemStack AddItem(ItemStack itemStack)
    {
        var local = new ItemStack(itemStack);
        foreach (var inv in GetIndividualInventories())
        {
            if (inv.AddItem(local) || local.Count == 0) return null;
        }
        return local;
    }

    public void RemoveItemSafe(ItemStack itemStack)
    {
        var local = new ItemStack(itemStack);
        foreach (var inv in GetIndividualInventories())
        {
            inv.RemoveItem(local);
            if (local.Count == 0) return;
        }
    }

    public IEnumerable<ItemStack> GetAllItems()
    {
        return GetIndividualInventories().SelectMany(inv => inv.GetAllItems());
    }

    public Dictionary<Item, int> GetItemCounts()
    {
        var res = new Dictionary<Item, int>();
        foreach (var inv in GetIndividualInventories())
        {
            var counts = inv.GetItemCounts();
            foreach (var kvp in counts)
            {
                if (res.ContainsKey(kvp.Key))
                    res[kvp.Key] += kvp.Value;
                else
                    res[kvp.Key] = kvp.Value;
            }
        }
        return res;
    }

    public IEnumerable<IInventory> GetIndividualInventories();
}
