using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class ToolTip : MonoBehaviour
{
    public static ToolTip instance;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Stats;
    public SingleChildLayoutController ItemDisplay;
    public float offsetScale = 1.75f;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        instance = this;
        canvas = GetComponentInParent<Canvas>();
    }

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = transform as RectTransform;
        gameObject.SetActive(false);
    }

    public void Display((string formatedName, string StatsString, IGridSource subGrid) t, Vector3 position, Vector3 slotSize)
    {
        gameObject.SetActive(true);

        Name.text = t.formatedName;
        Stats.text = t.StatsString;
        if(t.subGrid == null)
        {
            ItemDisplay.gameObject.SetActive(false);
        }
        else
        {
            ItemDisplay.gameObject.SetActive(true);
            ItemDisplay.Render(t.subGrid);
        }

        Canvas.ForceUpdateCanvases();

        var offset = (rectTransform.rect.size.ToVector3() + slotSize) / offsetScale;
        offset.y *= -1;
        offset *= canvas.scaleFactor;
        transform.position = position + offset;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
