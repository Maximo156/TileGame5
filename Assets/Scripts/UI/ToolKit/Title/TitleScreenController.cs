using UnityEngine;
using UnityEngine.UIElements;

public class TitleScreenController : MonoBehaviour
{
    public VisualTreeAsset SaveListEntryTeplate;
    public UIDocument MainDoc;

    MainScreenController mainController;
    NewSaveController newSaveController;
    LoadSaveController loadSaveController;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        var main = root.Q<VisualElement>("Main");
        var load = root.Q<VisualElement>("Load");
        var newSave = root.Q<VisualElement>("New");

        mainController = new MainScreenController(main, ViewNew, ViewLoad);
        newSaveController = new NewSaveController(newSave, ViewMain, ViewLoad);
        loadSaveController = new LoadSaveController(load, ViewMain, ViewNew, SaveListEntryTeplate);

        void ViewMain()
        {
            main.RemoveFromClassList("hidden");
            load.AddToClassList("hidden");
            newSave.AddToClassList("hidden");
        }

        void ViewLoad()
        {
            loadSaveController.UpdateList();
            load.RemoveFromClassList("hidden");
            main.AddToClassList("hidden");
            newSave.AddToClassList("hidden");
        }

        void ViewNew()
        {
            newSave.RemoveFromClassList("hidden");
            load.AddToClassList("hidden");
            main.AddToClassList("hidden");
        }
    }

    private void OnDisable()
    {
        
    }
}
