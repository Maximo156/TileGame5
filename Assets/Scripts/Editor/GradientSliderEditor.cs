using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(GradientSlider))]
public class GradientSliderEditor : SliderEditor
{
    private SerializedProperty Gradient;

    // Called when the Inspector is loaded
    // Usually when the according GameObject gets selected in the hierarchy
    protected override void OnEnable()
    {
        base.OnEnable();
        Gradient = serializedObject.FindProperty("Gradient");
    }

    // Kind of the Inspector's Update method
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();
        EditorGUILayout.PropertyField(Gradient);
        serializedObject.ApplyModifiedProperties();
    }
}
