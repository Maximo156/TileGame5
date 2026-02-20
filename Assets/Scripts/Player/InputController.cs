using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    EventSystem events;
    bool selectedGameobject;
    bool overGameObject;

    public bool AllowMovement => enabled && !selectedGameobject;
    public bool AllowClickInput => enabled && !overGameObject && !selectedGameobject;
    public bool AllowScrollInput => enabled && !overGameObject;

    void Awake()
    {
        events = EventSystem.current;
    }

    // Update is called once per frame
    void Update()
    {
        selectedGameobject = events.currentSelectedGameObject != null && events.currentSelectedGameObject.activeInHierarchy;
        overGameObject = events.IsPointerOverGameObject();
    }
}
