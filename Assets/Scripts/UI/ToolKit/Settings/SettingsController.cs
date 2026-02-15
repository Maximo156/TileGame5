using UnityEngine;
using UnityEngine.UIElements;

public class SettingsController : MonoBehaviour
{
    public UIDocument MainDoc;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        var main = root.Q<VisualElement>("Main");

        new MainSettingsController(main);
    }
}
