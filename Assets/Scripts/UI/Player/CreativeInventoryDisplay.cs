using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class CreativeInventoryDisplay : MonoBehaviour, IGridSource
{
    public ItemDrag ItemDrag;
    public SingleChildLayoutController display;

    string search = "";

    EventSystem eventSystem;

    private void Start()
    {
        eventSystem = EventSystem.current;
    }

    private void OnEnable()
    {
        Render();
    }

    public void Render()
    {
        display.Render(this, OnClick);
    }

    void OnClick(int index, IGridItem gridItem, PointerEventData _)
    {
        var item = gridItem as Item;
        ItemDrag.SetItem(item, item.MaxStackSize);
    }

    public IEnumerable<IGridItem> GetGridItems() => ItemRepository.Items.Where(i => string.IsNullOrEmpty(search) || i.name.ToLower().Contains(search));

    public void SetSearch(string search)
    {
        this.search = search.ToLower();
        Render();
    }
}
