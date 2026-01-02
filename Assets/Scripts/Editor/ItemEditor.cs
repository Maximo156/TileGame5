using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Item), true)]
public class ItemEditor : DisplaySpriteEditor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        // Draw default inspector
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        SerializedProperty itemBehaviors = serializedObject.FindProperty("Behaviors");

        // Add button that opens the popup
        var button = new Button()
        {
            text = "Add behaviour"
        };

        root.Add(button);
        button.clicked += () =>
        {
            var options = new List<string> { "Option A", "Option B", "Option C", "Another option" };

            var ItemBehaviorClasses = Utilities.GetAllConcreteSubclassesOf<ItemBehavior>().ToDictionary(t => t.Name, t => t);

            SearchablePopup.Show(GetScreenRect(button), ItemBehaviorClasses.Keys.ToList(), selected =>
            {
                ItemBehavior instance = (ItemBehavior)Activator.CreateInstance(ItemBehaviorClasses[selected]);

                int index = itemBehaviors.arraySize;
                itemBehaviors.InsertArrayElementAtIndex(index);

                SerializedProperty newElement = itemBehaviors.GetArrayElementAtIndex(index);
                newElement.managedReferenceValue = instance;

                serializedObject.ApplyModifiedProperties();
            });
        };


        return root;
    }

    public static Rect GetScreenRect(VisualElement element)
    {
        // Element's position in panel space
        var world = element.worldBound;

        // Panel's top-left relative to screen
        var panel = element.panel.visualTree.worldBound;

        // EditorWindow position in screen space
        var window = EditorWindow.focusedWindow.position;

        return new Rect(
            window.x + world.x - panel.x,
            window.y + world.y - panel.y + world.height, // + height to open below
            world.width,
            world.height
        );
    }
}

public class SearchablePopup : EditorWindow
{
    private Action<string> onSelected;
    private List<string> allOptions;
    private ListView listView;
    private TextField searchField;

    public static void Show(Rect activatorRect, List<string> options, Action<string> onSelected)
    {
        var window = CreateInstance<SearchablePopup>();
        window.allOptions = options;
        window.onSelected = onSelected;

        window.ShowAsDropDown(activatorRect, new Vector2(300, 400));
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;

        // --- CONTAINER WITH STYLES ---
        var container = new VisualElement();
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;

        container.style.borderTopWidth = 1;
        container.style.borderBottomWidth = 1;
        container.style.borderLeftWidth = 1;
        container.style.borderRightWidth = 1;

        container.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        container.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        container.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        container.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        container.StretchToParentSize();

        root.Add(container);

        searchField = new TextField();
        searchField.style.marginBottom = 4;
        searchField.RegisterValueChangedCallback(evt => RefreshList(evt.newValue));
        container.Add(searchField);

        listView = new ListView();
        listView.makeItem = () => new Label();
        listView.bindItem = (e, i) =>
        {
            (e as Label).text = listView.itemsSource[i]?.ToString();
        };
        listView.selectionChanged += objects =>
        {
            string selected = objects.First().ToString();
            onSelected?.Invoke(selected);
            Close();
        };
        listView.selectionType = SelectionType.Single;

        container.Add(listView);

        RefreshList("");
    }

    private void RefreshList(string search)
    {
        if (string.IsNullOrEmpty(search))
            listView.itemsSource = allOptions;
        else
            listView.itemsSource = allOptions.Where(o =>
                o.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();

        listView.Rebuild();
    }
}
