#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueChoiceData))]
public class DialogueChoiceDataDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty titleProperty = property.FindPropertyRelative("inspectorChoiceTitle");

        string title = "Choice";
        if (titleProperty != null && !string.IsNullOrWhiteSpace(titleProperty.stringValue))
        {
            title = titleProperty.stringValue;
        }

        EditorGUI.PropertyField(position, property, new GUIContent(title), true);
    }
}
#endif