using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public interface ITooltipSource
{
    public bool TryGetTooltipInfo(out string title, out string body, out IGridSource subGrid);
}

public class DynamicTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public enum TrackingStyle
    {
        Static,
        FollowCursor
    }
    public TrackingStyle Tracking = TrackingStyle.Static;
    private ITooltipSource source;
    private Canvas parent;
    public void Awake()
    {
        parent = GetComponentInParent<Canvas>();
        foreach (Component comp in GetComponents<Component>())
        {
            if (comp is ITooltipSource)
            {
                source = comp as ITooltipSource;
            }
        }
        if (source == null) throw new InvalidOperationException(typeof(DynamicTooltip) + " requires at least one component that implements " + typeof(ITooltipSource));
    }

    private void OnDisable()
    {
        if (inside)
        {
            inside = false;
            ToolTip.instance.Hide();
        }
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        if (Tracking == TrackingStyle.FollowCursor)
        {
            FollowCursor(eventData);
        }
    }

    bool inside;
    public void OnPointerEnter(PointerEventData eventData)
    {
        inside = true;
        if (source.TryGetTooltipInfo(out var title, out var body, out var items))
        {
            ToolTip.instance.Display((title, body, items), GetPosition(transform.position), (transform as RectTransform).rect.size / 2);
            if(Tracking == TrackingStyle.FollowCursor)
            {
                FollowCursor(eventData);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
        ToolTip.instance.Hide();
    }

    private void FollowCursor(PointerEventData eventData)
    {
        var offset = (ToolTip.instance.transform as RectTransform).rect.size / 2;
        offset.y *= -1;
        ToolTip.instance.transform.position = eventData.position + offset;
    }

    private Vector3 GetPosition(Vector3 position)
    {
        if(parent.renderMode == RenderMode.WorldSpace)
        {
            return parent.worldCamera.WorldToScreenPoint(position);
        }
        return position;
    }
}
