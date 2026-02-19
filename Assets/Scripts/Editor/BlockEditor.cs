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
        return root;
    }
}