using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridItemWithListDisplay : GridItemDisplay
{
    public SingleChildLayoutController List;
    public override void SetDisplay(IGridItem item, int selfIndex, Action<int, IGridItem, PointerEventData> onClick, Action<int, IGridItem, PointerEventData> onMouseUp)
    {
        if(item is IGridSource source)
        {
            List.Render(source);
        }
        base.SetDisplay(item, selfIndex, onClick, onMouseUp);
    }
}
