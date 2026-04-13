#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestData))]
public class QuestDataToolsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        QuestData quest = (QuestData)target;

        EditorGUILayout.HelpBox(
            "For faster editing, use Tools > HeroTest > Quests > Open Editor.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open In Quest Tools"))
            {
                QuestToolsWindow.OpenAndSelectQuest(quest);
            }

            if (GUILayout.Button("Generate IDs"))
            {
                Undo.RecordObject(quest, "Generate Quest IDs");
                QuestToolsEditorUtility.GenerateIds(quest);
                EditorUtility.SetDirty(quest);
            }

            if (GUILayout.Button("Validate Quest"))
            {
                QuestToolsEditorUtility.LogValidationToConsole(quest);
            }
        }

        EditorGUILayout.Space(6f);
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(QuestManager))]
public class QuestManagerToolsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        QuestManager manager = (QuestManager)target;

        EditorGUILayout.HelpBox(
            "QuestManager uses the serialized allQuestData array. You can auto-fill it from all QuestData assets in the project.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Quest Tools"))
            {
                QuestToolsWindow.OpenWindow();
            }

            if (GUILayout.Button("Sync All QuestData"))
            {
                Undo.RecordObject(manager, "Sync QuestManager QuestData");
                QuestToolsEditorUtility.SyncQuestManager(manager);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Validate All Quests"))
            {
                QuestToolsEditorUtility.LogValidationForAllQuests();
            }
        }

        EditorGUILayout.Space(6f);
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(QuestTriggerZone))]
public class QuestTriggerZoneToolsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty zoneId = serializedObject.FindProperty("zoneId");
        SerializedProperty notifyObjectiveOnEnter = serializedObject.FindProperty("notifyObjectiveOnEnter");
        SerializedProperty questsToAcceptOnEnter = serializedObject.FindProperty("questsToAcceptOnEnter");

        EditorGUILayout.HelpBox(
            "Use zoneId for ReachTriggerZone objectives. questsToAcceptOnEnter is useful for auto-starting quests when the player enters the trigger.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Quest Tools"))
            {
                QuestToolsWindow.OpenWindow();
            }

            if (GUILayout.Button("Use GameObject Name As ZoneId"))
            {
                zoneId.stringValue = QuestToolsEditorUtility.SanitizeId(((QuestTriggerZone)target).gameObject.name, "zone");
            }

            if (GUILayout.Button("Ping Accepted Quests"))
            {
                bool pingedAny = false;

                for (int i = 0; i < questsToAcceptOnEnter.arraySize; i++)
                {
                    Object questObj = questsToAcceptOnEnter.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (questObj != null)
                    {
                        EditorGUIUtility.PingObject(questObj);
                        pingedAny = true;
                    }
                }

                if (!pingedAny)
                {
                    Debug.Log("QuestTriggerZone: no QuestData assets assigned in questsToAcceptOnEnter.", target);
                }
            }
        }

        EditorGUILayout.Space(6f);

        EditorGUILayout.PropertyField(zoneId);
        EditorGUILayout.PropertyField(notifyObjectiveOnEnter);
        EditorGUILayout.PropertyField(questsToAcceptOnEnter, true);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif