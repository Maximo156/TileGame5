using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BiomeInfoEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset controllsDisplayTree;


    private BiomeInfoEditorDisplay editorDisplay;

    private VisualElement BuiltControllsDisplay;

    [MenuItem("Window/UI Toolkit/BiomeInfoEditor")]
    public static void ShowGui()
    {
        BiomeInfoEditor wnd = GetWindow<BiomeInfoEditor>();
        wnd.titleContent = new GUIContent("Biome Info");
    }


    public void CreateGUI()
    {
        var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);

        // Add the view to the visual tree by adding it as a child to the root element.
        rootVisualElement.Add(splitView);

        initLeftPane(splitView);

        BuiltControllsDisplay = controllsDisplayTree.Instantiate();
        splitView.Add(BuiltControllsDisplay);
    }

    void initLeftPane(VisualElement splitView)
    {
        // Get a list of all RealmBiomeInfo in the project
        var allObjectGuids = AssetDatabase.FindAssets("t:RealmBiomeInfo");
        var allObjects = new List<RealmBiomeInfo>();
        foreach (var guid in allObjectGuids)
        {
            allObjects.Add(AssetDatabase.LoadAssetAtPath<RealmBiomeInfo>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        var leftPane = new ListView();
        splitView.Add(leftPane);

        leftPane.makeItem = () => new Label();
        leftPane.bindItem = (item, index) => { (item as Label).text = allObjects[index].name; };
        leftPane.itemsSource = allObjects;

        leftPane.selectionChanged += OnInfoSelectionChange;
    }

    private void OnInfoSelectionChange(IEnumerable<object> selectedItems)
    {

        // Get the selected sprite and display it.
        var enumerator = selectedItems.GetEnumerator();
        if (enumerator.MoveNext())
        {
            var selectedInfo = enumerator.Current as RealmBiomeInfo;
            if (selectedInfo != null)
            {
                editorDisplay?.OnDisable();
                editorDisplay = new BiomeInfoEditorDisplay(BuiltControllsDisplay, selectedInfo, Repaint);
            }
        }
    }

    private void OnDisable()
    {
        editorDisplay?.OnDisable();
    }
}
