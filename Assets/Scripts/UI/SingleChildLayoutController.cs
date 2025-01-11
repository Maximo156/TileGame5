using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IClickable
{
    public void OnClick(PointerEventData eventData);
}

public interface IGridItem
{
    public Sprite GetSprite();
    public string GetString();

    public Color GetColor();

    public float? GetFullness() {
        return null;
    }

    public (string, string) GetTooltipString();
}

public interface IGridSource
{
    public IEnumerable<IGridItem> GetGridItems();
}

public interface IGridClickListener
{
    public void OnClick(IGridItem item);
}

[RequireComponent(typeof(LayoutGroup))]
public class SingleChildLayoutController : MonoBehaviour
{
    public GameObject GridItemDisplayPrefab;

    private List<GridItemDisplay> currentChildren = new List<GridItemDisplay>();

    public GridLayoutGroup GridLayout;
    int layoutGroupConstraint = 5;

    private void Start()
    {
        if(GridLayout is not null)
        {
            layoutGroupConstraint = GridLayout.constraintCount;
        }
    }

    public void Clear()
    {
        SetupChildren(0);
    }

    public void Render(IGridSource source, Action<int, IGridItem, PointerEventData> onClick = null, Action<int, IGridItem, PointerEventData> onMouseUp = null)
    {
        var items = source.GetGridItems().ToList();

        if (GridLayout is not null)
        {
            GridLayout.constraintCount = layoutGroupConstraint > items.Count ? items.Count : layoutGroupConstraint;
        }

        SetupChildren(items.Count);
        for(int i = 0; i< items.Count; i++)
        {
            currentChildren[i].SetDisplay(items[i], i, onClick, onMouseUp);
        }
    }

    private void SetupChildren(int count)
    {
        for(int i = transform.childCount - 1; i >= count; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        while(currentChildren.Count > transform.childCount)
        {
            currentChildren.RemoveAt(currentChildren.Count - 1);
        }
        while (currentChildren.Count < transform.childCount)
        {
            currentChildren.Add(transform.GetChild(currentChildren.Count).GetComponent<GridItemDisplay>());
        }
        while(currentChildren.Count < count)
        {
            currentChildren.Add(Instantiate(GridItemDisplayPrefab, transform).GetComponent<GridItemDisplay>());
        }
    }
}
