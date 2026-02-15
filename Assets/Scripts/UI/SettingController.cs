using UnityEngine;

public class SettingController : MonoBehaviour
{
    public GameObject SettingsUI;
    public GameObject GameUI;

    public void OnToggleSettings()
    {
        SettingsUI.SetActive(!SettingsUI.activeSelf);
        GameUI.SetActive(!SettingsUI.activeSelf);
    }
}
