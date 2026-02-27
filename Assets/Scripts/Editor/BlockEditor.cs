using ComposableBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Block), true)]
[CanEditMultipleObjects]
public class BlockEditor : DisplaySpriteEditor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();


        SerializedProperty idProperty = serializedObject.FindProperty("Id");

        var button = new Button()
        {
            text = "Reset Id"
        };

        root.Add(button);
        button.clicked += () =>
        {
            idProperty.intValue = 0;
            serializedObject.ApplyModifiedProperties();
        };

        // Draw default inspector
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        SerializedProperty itemBehaviors = serializedObject.FindProperty(nameof(Block.Behaviors));

        // Add button that opens the popup
        var behaviourButton = new Button()
        {
            text = "Add behaviour"
        };

        root.Add(behaviourButton);
        behaviourButton.clicked += () =>
        {
            var BlockBehaviorClasses = BlockBehaviour.Types.ToDictionary(t => t.Name, t => t);

            SearchablePopup.Show(Utilities.GetScreenRect(button), BlockBehaviorClasses.Keys.ToList(), selected =>
            {
                BlockBehaviour instance = (BlockBehaviour)Activator.CreateInstance(BlockBehaviorClasses[selected]);

                int index = itemBehaviors.arraySize;
                itemBehaviors.InsertArrayElementAtIndex(index);

                SerializedProperty newElement = itemBehaviors.GetArrayElementAtIndex(index);
                newElement.managedReferenceValue = instance;

                serializedObject.ApplyModifiedProperties();
            });
        };

        return root;
    }
}