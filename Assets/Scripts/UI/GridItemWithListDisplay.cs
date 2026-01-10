using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class GridItemWithListDisplay : GridItemDisplay
{
    public SingleChildLayoutController List;
    public override void SetDisplay(IGridItem item, 
        int selfIndex, 
        Action<int, IGridItem, PointerEventData> onClick, 
        Action<int, IGridItem, PointerEventData> onMouseUp, 
        DisplayOverride overrideDisplay)
    {
        if(item is IGridSource source)
        {
            List.Render(source);
        }
        base.SetDisplay(item, selfIndex, onClick, onMouseUp, overrideDisplay);
    }
}
