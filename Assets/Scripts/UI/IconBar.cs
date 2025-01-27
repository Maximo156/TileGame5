using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutGroup))]
public class IconBar : MonoBehaviour, ITooltipSource
{
    public Sprite Sprite;
    public int IconValue;

    private EntityVariableStat _stat;
    public EntityVariableStat stat { get => _stat; 
        set 
        {
            if(_stat != null)
            {
                _stat.OnChange -= Render;
            }
            _stat = value;
            _stat.OnChange += Render;
        }
    }

    private GameObject icon;
    private float curValue;
    private float maxValue;
    private List<Image> images = new();
    public void Awake()
    {
        icon = new GameObject("icon");
        icon.AddComponent<RectTransform>();
        icon.AddComponent<Image>().sprite = Sprite;
    }

    public void Render()
    {
        maxValue = stat.MaxValue;
        var value = stat.current;
        if (curValue == value) return;
        curValue = value;

        var count = Mathf.CeilToInt(value / IconValue);
        var solidCount = Mathf.FloorToInt(value / IconValue);
        SpawnMaxChildren(count);

        for (int i = 0; i < transform.childCount; i++)
        {
            if(i < solidCount)
            {
                images[i].color = Color.white;
            }
            else if(i < count)
            {
                images[i].color = new Color(1, 1, 1, value % IconValue / IconValue);
            }
            else
            {
                images[i].color = new Color(1, 1, 1, 0);
            }
        }
    }

    public void SpawnMaxChildren(int count)
    {
        if(transform.childCount != images.Count)
        {
            images = new();
            for (int i = 0; i < transform.childCount; i++)
            {
                images.Add(transform.GetChild(i).GetComponent<Image>());
            }
        }
        while (transform.childCount < count)
        {
            images.Add(Instantiate(icon, transform).GetComponent<Image>());
        }
    }

    public bool TryGetTooltipInfo(out string title, out string body, out IGridSource items)
    {
        title = name.Replace("Bar", "").Trim();
        body = $"{(int)curValue}/{(int)maxValue}";
        items = null;
        return true;
    }
}
