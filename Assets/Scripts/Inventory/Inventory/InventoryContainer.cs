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

    public IEnumerable<ItemStack> GetAllItems(bool returnEmpty = true)
    {
        return GetIndividualInventories().SelectMany(inv => inv.GetAllItems(returnEmpty));
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

    public List<ItemStack> RemoveAllItems()
    {
        var result = new List<ItemStack>();
        foreach (var inv in GetIndividualInventories())
        {
            for(int i = 0; i < inv.Count; i++)
            {
                result.Add(inv.RemoveItemIndex(i));
            }
        }
        return result;
    }

    public IEnumerable<IInventory> GetIndividualInventories();
}
