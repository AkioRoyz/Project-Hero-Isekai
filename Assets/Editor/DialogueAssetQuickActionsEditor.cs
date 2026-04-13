#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataToolsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueData dialogue = (DialogueData)target;

        EditorGUILayout.HelpBox("For faster editing, use Tools > HeroTest > Dialogue > Open Editor.", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open In Dialogue Tools"))
                DialogueToolsWindow.OpenAndSelectDialogue(dialogue);

            if (GUILayout.Button("Validate Dialogue"))
            {
                dialogue.ValidateDialogue();
                EditorUtility.SetDirty(dialogue);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.Space(6f);
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(DialogueSpeakerData))]
public class DialogueSpeakerDataToolsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueSpeakerData speaker = (DialogueSpeakerData)target;

        EditorGUILayout.HelpBox("Open the shared Dialogue Tools window to edit speaker assets faster.", MessageType.Info);

        if (GUILayout.Button("Open In Dialogue Tools"))
            DialogueToolsWindow.OpenAndSelectSpeaker(speaker);

        EditorGUILayout.Space(6f);
        DrawDefaultInspector();
    }
}
#endif
