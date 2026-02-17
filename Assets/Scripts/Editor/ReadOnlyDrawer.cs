using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyPropertyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Disable the GUI, making the field non-interactive
        GUI.enabled = false;

        // Draw the property field as usual, but it will be greyed out
        EditorGUI.PropertyField(position, property, label, true);

        // Re-enable the GUI for subsequent fields
        GUI.enabled = true;
    }
}

