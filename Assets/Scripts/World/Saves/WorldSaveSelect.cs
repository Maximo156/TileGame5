using UnityEngine;

public class WorldSaveSelect : MonoBehaviour
{
    public static string SelectedSaveName => instance.selectedSaveName;

    public string selectedSaveName;

    static WorldSaveSelect instance;

    private void Awake()
    {
        instance = this;
    }
}
