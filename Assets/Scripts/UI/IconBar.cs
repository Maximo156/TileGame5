using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutGroup))]
public class IconBar : MonoBehaviour, ITooltipSource
{
    public Sprite Sprite;
    public int IconValue;

    private EntityStat _stat;
    public EntityStat stat { get => _stat; 
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
    public void Awake()
    {
        icon = new GameObject("icon");
        icon.AddComponent<RectTransform>();
        icon.AddComponent<Image>().sprite = Sprite;
    }

    public void Render()
    {
        maxValue = stat.Max;
        var value = stat.current;
        if (curValue == value) return;
        curValue = value;

        for (int i = 0; i< transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        var count = (int)(value / IconValue);
        if (value % IconValue != 0)
        {
            var lastImg = Instantiate(icon, transform).GetComponent<Image>();
            lastImg.color -= new Color(0, 0, 0, 1 - (value % IconValue / IconValue));
        }
        for (int i = 0; i < count; i++)
        {
            Instantiate(icon, transform);
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
