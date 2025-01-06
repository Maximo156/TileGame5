using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[ExecuteAlways]
public class RadialLayout : MonoBehaviour
{
    public int Radius;
    public GameObject GridItemDisplayPrefab;

    private List<GridItemDisplay> currentChildren = new List<GridItemDisplay>();

    public void Render(IGridSource source, Action<int, IGridItem, PointerEventData> onClick)
    {
        var items = source.GetGridItems().ToList();
        SetupChildren(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            currentChildren[i].SetDisplay(items[i], i, onClick);
        }
        ArrangeChildren();
    }

    private void SetupChildren(int count)
    {
        for (int i = transform.childCount - 1; i >= count; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        while (currentChildren.Count > transform.childCount)
        {
            currentChildren.RemoveAt(currentChildren.Count - 1);
        }
        while (currentChildren.Count < transform.childCount)
        {
            currentChildren.Add(transform.GetChild(currentChildren.Count).GetComponent<GridItemDisplay>());
        }
        while (currentChildren.Count < count)
        {
            currentChildren.Add(Instantiate(GridItemDisplayPrefab, transform).GetComponent<GridItemDisplay>());
        }
    }

    private void ArrangeChildren()
    {
        float radiansOfSeperation = (Mathf.PI * 2) / transform.childCount;
        for (int i = 0; i < transform.childCount; i++)
        {
            float x = Mathf.Sin(radiansOfSeperation * i) * Radius;
            float y = Mathf.Cos(radiansOfSeperation * i) * Radius;
            (transform.GetChild(i) as RectTransform).anchoredPosition = new Vector3(x, y, 0);
        }
    }

    private void Update()
    {
        ArrangeChildren();
    }

    private void OnValidate()
    {
        ArrangeChildren();
    }
}
