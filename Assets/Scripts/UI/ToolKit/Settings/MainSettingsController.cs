using UnityEngine;
using UnityEngine.UIElements;

public class MainSettingsController
{
    public MainSettingsController(VisualElement element)
    {
        element.Q<Button>("Exit").clicked += OnExitClicked;
    }

    void OnExitClicked()
    {
        WorldSave.ExitSave();
    }
}
