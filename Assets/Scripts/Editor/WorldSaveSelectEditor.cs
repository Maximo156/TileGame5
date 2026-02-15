using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(WorldSaveSelect), true)]
[CanEditMultipleObjects]
public class WorldSaveSelectEditor : Editor
{
    SerializedProperty selectedSaveNameProp;
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        // Cache property
        selectedSaveNameProp = serializedObject.FindProperty("selectedSaveName");

        // Load save names
        var nameOptions = WorldSave
            .LoadSaves()
            .Select(s => s.worldName)
            .ToList();

        // Ensure current value exists in list (important when saves deleted)
        if (!string.IsNullOrEmpty(selectedSaveNameProp.stringValue) &&
            !nameOptions.Contains(selectedSaveNameProp.stringValue))
        {
            nameOptions.Insert(0, selectedSaveNameProp.stringValue);
        }

        // Create dropdown
        var dropdown = new DropdownField(
            label: "Selected Save",
            choices: nameOptions,
            defaultIndex: Mathf.Max(0, nameOptions.IndexOf(selectedSaveNameProp.stringValue))
        );

        // Write back when changed
        dropdown.RegisterValueChangedCallback(evt =>
        {
            serializedObject.Update();
            selectedSaveNameProp.stringValue = evt.newValue;
            serializedObject.ApplyModifiedProperties();
        });

        // Keep UI synced if value changes externally
        dropdown.BindProperty(selectedSaveNameProp);

        root.Add(dropdown);

        return root;
    }
}
