using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridItemDisplay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ITooltipSource
{
    public delegate int CleanupOverride(Image img, TextMeshProUGUI text, Slider slider);
    public delegate CleanupOverride DisplayOverride(IGridItem item, Image img, TextMeshProUGUI text, Slider slider);

    public Image img;
    public TextMeshProUGUI text;
    public Slider Durability;

    IGridItem displayed;

    Action<int, IGridItem, PointerEventData> onClick;
    Action<int, IGridItem, PointerEventData> onMouseUp;
    DisplayOverride overrideDisplay;
    CleanupOverride cleanupOverride;
    int selfIndex;

    public virtual void SetDisplay(
        IGridItem item, 
        int selfIndex, 
        Action<int, IGridItem, PointerEventData> onClick = null, 
        Action<int, IGridItem, PointerEventData> onMouseUp = null,
        DisplayOverride overrideDisplay = null)
    {
        this.onClick = onClick;
        this.onMouseUp = onMouseUp;
        this.selfIndex = selfIndex;
        this.overrideDisplay = overrideDisplay;
        displayed = item;
        Render();
    }

    private void Render()
    {
        cleanupOverride?.Invoke(img, text, Durability);
        SetImage();
        SetText();
        SetDurability();
        cleanupOverride = overrideDisplay?.Invoke(displayed, img, text, Durability);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (displayed is IClickable selfClickable)
        {
            selfClickable.OnClick(eventData);
        }
        else
        {
            onClick?.Invoke(selfIndex, displayed, eventData);
        }
    }

    public bool TryGetTooltipInfo(out string title, out string body, out IGridSource items)
    {
        items = displayed as IGridSource;
        (title, body) = ("", "");
        if (displayed != null) 
        {
            (title, body) = displayed.GetTooltipString();

            return true;
        }
        return false;
    }

    private void SetDurability()
    {
        if(Durability is not null)
        {
            if(displayed is not null)
            {
                var fullness = displayed.GetFullness();
                if (fullness is null or 1)
                {
                    Durability.gameObject.SetActive(false);
                }
                else
                {
                    Durability.gameObject.SetActive(true);
                    Durability.value = fullness.Value;
                }
            }
            else
            {
                Durability.gameObject.SetActive(false);
            }
        }
    }

    private void SetImage()
    {
        if(img is not null)
        {
            if (displayed != null)
            {
                img.enabled = true;
                img.sprite = displayed.Sprite;
                img.color = displayed.GetColor();
            }
            else
            {
                img.enabled = false;
            }
        }
    }

    private void SetText()
    {
        if (text is not null)
        {
            if (displayed != null)
            {
                text.enabled = true;
                text.text = displayed.GetString() ?? "";
            }
            else
            {
                text.enabled = false;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onMouseUp?.Invoke(selfIndex, displayed, eventData);
    }
}
