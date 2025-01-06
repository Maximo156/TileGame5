using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleTooltip : MonoBehaviour, ITooltipSource
{
    public string title;
    public string desc;

    public bool TryGetTooltipInfo(out string title, out string body, out IGridSource items)
    {
        items = null;
        (title, body) = (this.title, this.desc);
        return true;
    }
}
