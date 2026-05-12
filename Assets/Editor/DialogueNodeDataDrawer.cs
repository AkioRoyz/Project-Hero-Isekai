#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueNodeData))]
public class DialogueNodeDataDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty titleProperty = property.FindPropertyRelative("inspectorNodeTitle");
        SerializedProperty backgroundProperty = property.FindPropertyRelative("dialogueBackground");

        string title = "Node";
        if (titleProperty != null && !string.IsNullOrWhiteSpace(titleProperty.stringValue))
        {
            title = titleProperty.stringValue;
        }

        string backgroundSuffix = backgroundProperty != null && backgroundProperty.objectReferenceValue != null ? "  • BG" : string.Empty;
        string displayLabel = $"{title} [{GetArrayIndex(property)}]{backgroundSuffix}";
        EditorGUI.PropertyField(position, property, new GUIContent(displayLabel), true);
    }

    private int GetArrayIndex(SerializedProperty property)
    {
        string path = property.propertyPath;
        int arrayDataIndex = path.LastIndexOf(".Array.data[", System.StringComparison.Ordinal);

        if (arrayDataIndex < 0)
            return -1;

        int start = arrayDataIndex + ".Array.data[".Length;
        int end = path.IndexOf(']', start);

        if (end < 0)
            return -1;

        string indexString = path.Substring(start, end - start);

        if (int.TryParse(indexString, out int index))
            return index;

        return -1;
    }
}
#endif