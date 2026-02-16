using UnityEngine;
using UnityEngine.EventSystems;

public class DeleteSlot : MonoBehaviour, IPointerClickHandler
{
    public ItemDrag itemDrag;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        itemDrag.ClearItem();
    }
}
