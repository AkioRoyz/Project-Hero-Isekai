#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class QuestToolsWindow : EditorWindow
{
    private const float LeftPanelWidth = 320f;

    private Vector2 leftScroll;
    private Vector2 rightScroll;
    private string searchQuery = string.Empty;

    private List<QuestData> cachedQuests = new List<QuestData>();
    private QuestData selectedQuest;
    private SerializedObject selectedQuestSerializedObject;

    private bool showBase = true;
    private bool showFlags = true;
    private bool showTexts = true;
    private bool showAvailability = true;
    private bool showSteps = true;
    private bool showValidation = true;

    [MenuItem("Tools/HeroTest/Quests/Open Editor")]
    public static void OpenWindow()
    {
        QuestToolsWindow window = GetWindow<QuestToolsWindow>("Quest Tools");
        window.minSize = new Vector2(1100f, 650f);
        window.RefreshQuestCache();
        window.Show();
    }

    [MenuItem("Tools/HeroTest/Quests/Create New Quest Asset")]
    public static void CreateNewQuestAssetMenu()
    {
        QuestData quest = QuestToolsEditorUtility.CreateQuestAsset();
        OpenAndSelectQuest(quest);
    }

    [MenuItem("Tools/HeroTest/Quests/Validate All Quest Assets")]
    public static void ValidateAllQuestAssetsMenu()
    {
        QuestToolsEditorUtility.LogValidationForAllQuests();
        OpenWindow();
    }

    [MenuItem("Tools/HeroTest/Quests/Sync All QuestData To QuestManagers In Open Scenes")]
    public static void SyncManagersMenu()
    {
        int count = QuestToolsEditorUtility.SyncAllQuestManagersInOpenScenes();
        Debug.Log($"Quest Tools: synced QuestData array on {count} QuestManager object(s) in open scenes.");
        OpenWindow();
    }

    public static void OpenAndSelectQuest(QuestData quest)
    {
        QuestToolsWindow window = GetWindow<QuestToolsWindow>("Quest Tools");
        window.minSize = new Vector2(1100f, 650f);
        window.RefreshQuestCache();
        window.SelectQuest(quest);
        window.Show();
        window.Focus();
    }

    private void OnEnable()
    {
        RefreshQuestCache();
        Selection.selectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= HandleSelectionChanged;
    }

    private void HandleSelectionChanged()
    {
        if (Selection.activeObject is QuestData quest)
        {
            SelectQuest(quest);
            Repaint();
        }
    }

    private void SelectQuest(QuestData quest)
    {
        selectedQuest = quest;
        selectedQuestSerializedObject = selectedQuest != null ? new SerializedObject(selectedQuest) : null;
    }

    private void RefreshQuestCache()
    {
        cachedQuests = QuestToolsEditorUtility.FindAllQuestAssets();

        if (selectedQuest == null && cachedQuests.Count > 0)
        {
            SelectQuest(cachedQuests[0]);
        }
        else if (selectedQuest != null && !cachedQuests.Contains(selectedQuest))
        {
            SelectQuest(cachedQuests.Count > 0 ? cachedQuests[0] : null);
        }

        Repaint();
    }

    private void OnGUI()
    {
        DrawTopToolbar();

        Rect fullRect = GUILayoutUtility.GetRect(position.width, position.height - 30f);
        Rect leftRect = new Rect(fullRect.x, fullRect.y, LeftPanelWidth, fullRect.height);
        Rect rightRect = new Rect(fullRect.x + LeftPanelWidth + 6f, fullRect.y, fullRect.width - LeftPanelWidth - 6f, fullRect.height);

        DrawQuestListPanel(leftRect);
        DrawQuestEditorPanel(rightRect);
    }

    private void DrawTopToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RefreshQuestCache();
            }

            if (GUILayout.Button("Create Quest", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            {
                QuestData quest = QuestToolsEditorUtility.CreateQuestAsset();
                RefreshQuestCache();
                SelectQuest(quest);
            }

            if (GUILayout.Button("Validate All", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            {
                QuestToolsEditorUtility.LogValidationForAllQuests();
            }

            if (GUILayout.Button("Sync QuestManagers", EditorStyles.toolbarButton, GUILayout.Width(140f)))
            {
                int count = QuestToolsEditorUtility.SyncAllQuestManagersInOpenScenes();
                Debug.Log($"Quest Tools: synced QuestData array on {count} QuestManager object(s) in open scenes.");
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Search:", GUILayout.Width(50f));
            string newSearch = GUILayout.TextField(searchQuery, EditorStyles.toolbarTextField, GUILayout.Width(240f));
            if (!string.Equals(newSearch, searchQuery, StringComparison.Ordinal))
            {
                searchQuery = newSearch;
            }
        }
    }

    private void DrawQuestListPanel(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        EditorGUILayout.LabelField("Quest Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        List<QuestData> filtered = cachedQuests
            .Where(QuestMatchesSearch)
            .ToList();

        EditorGUILayout.LabelField($"Found: {filtered.Count}", EditorStyles.miniLabel);
        EditorGUILayout.Space(6f);

        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox("No QuestData assets found.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < filtered.Count; i++)
            {
                DrawQuestListItem(filtered[i]);
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private bool QuestMatchesSearch(QuestData quest)
    {
        if (quest == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return true;
        }

        string needle = searchQuery.Trim();
        string assetPath = AssetDatabase.GetAssetPath(quest);

        return (!string.IsNullOrWhiteSpace(quest.name) && quest.name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            || (!string.IsNullOrWhiteSpace(quest.QuestId) && quest.QuestId.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            || (!string.IsNullOrWhiteSpace(assetPath) && assetPath.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void DrawQuestListItem(QuestData quest)
    {
        bool isSelected = selectedQuest == quest;
        Color oldColor = GUI.color;
        if (isSelected)
        {
            GUI.color = new Color(0.75f, 0.9f, 1f);
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button(quest.name, EditorStyles.miniButton))
            {
                SelectQuest(quest);
                GUI.FocusControl(null);
            }

            EditorGUILayout.LabelField("QuestId", string.IsNullOrWhiteSpace(quest.QuestId) ? "<empty>" : quest.QuestId, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(quest), EditorStyles.miniLabel);
        }

        GUI.color = oldColor;
    }

    private void DrawQuestEditorPanel(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);

        if (selectedQuest == null)
        {
            EditorGUILayout.HelpBox("Select a QuestData asset from the list on the left.", MessageType.Info);
            GUILayout.EndArea();
            return;
        }

        if (selectedQuestSerializedObject == null || selectedQuestSerializedObject.targetObject != selectedQuest)
        {
            selectedQuestSerializedObject = new SerializedObject(selectedQuest);
        }

        selectedQuestSerializedObject.Update();

        DrawSelectedQuestHeader();

        List<QuestValidationMessage> validation = QuestToolsEditorUtility.ValidateQuest(selectedQuest);

        showValidation = EditorGUILayout.Foldout(showValidation, $"Validation ({validation.Count})", true);
        if (showValidation)
        {
            if (validation.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation issues found.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < validation.Count; i++)
                {
                    EditorGUILayout.HelpBox(validation[i].Text, validation[i].Type);
                }
            }
        }

        EditorGUILayout.Space(6f);

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        DrawQuestProperties(selectedQuestSerializedObject);

        EditorGUILayout.EndScrollView();

        if (selectedQuestSerializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(selectedQuest);
        }

        GUILayout.EndArea();
    }

    private void DrawSelectedQuestHeader()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(selectedQuest.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedQuest), EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping"))
                {
                    EditorGUIUtility.PingObject(selectedQuest);
                    Selection.activeObject = selectedQuest;
                }

                if (GUILayout.Button("Save"))
                {
                    selectedQuestSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    AssetDatabase.SaveAssets();
                    RefreshQuestCache();
                }

                if (GUILayout.Button("Generate / Normalize IDs"))
                {
                    Undo.RecordObject(selectedQuest, "Generate Quest IDs");
                    selectedQuestSerializedObject.Update();
                    QuestToolsEditorUtility.GenerateIds(selectedQuestSerializedObject);
                    selectedQuestSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    AssetDatabase.SaveAssets();
                    RefreshQuestCache();
                }

                if (GUILayout.Button("Validate To Console"))
                {
                    QuestToolsEditorUtility.LogValidationToConsole(selectedQuest);
                }

                if (GUILayout.Button("Duplicate Asset"))
                {
                    string sourcePath = AssetDatabase.GetAssetPath(selectedQuest);
                    string newPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(sourcePath) ?? "Assets", selectedQuest.name + "_Copy.asset").Replace("\\", "/"));
                    AssetDatabase.CopyAsset(sourcePath, newPath);
                    AssetDatabase.SaveAssets();
                    RefreshQuestCache();

                    QuestData duplicated = AssetDatabase.LoadAssetAtPath<QuestData>(newPath);
                    if (duplicated != null)
                    {
                        QuestToolsEditorUtility.GenerateIds(duplicated);
                        SelectQuest(duplicated);
                        Selection.activeObject = duplicated;
                        EditorGUIUtility.PingObject(duplicated);
                    }
                }
            }
        }
    }

    private void DrawQuestProperties(SerializedObject so)
    {
        SerializedProperty questId = so.FindProperty("questId");
        SerializedProperty questType = so.FindProperty("questType");
        SerializedProperty startType = so.FindProperty("startType");
        SerializedProperty repeatMode = so.FindProperty("repeatMode");

        SerializedProperty hiddenUntilStarted = so.FindProperty("hiddenUntilStarted");
        SerializedProperty canRestartAfterFail = so.FindProperty("canRestartAfterFail");
        SerializedProperty notifyOnAccept = so.FindProperty("notifyOnAccept");
        SerializedProperty notifyOnComplete = so.FindProperty("notifyOnComplete");

        SerializedProperty questTitle = so.FindProperty("questTitle");
        SerializedProperty shortDescription = so.FindProperty("shortDescription");
        SerializedProperty fullDescription = so.FindProperty("fullDescription");

        SerializedProperty requiredPlayerLevel = so.FindProperty("requiredPlayerLevel");
        SerializedProperty requiredItemId = so.FindProperty("requiredItemId");
        SerializedProperty requiredCompletedQuestId = so.FindProperty("requiredCompletedQuestId");

        SerializedProperty steps = so.FindProperty("steps");

        showBase = EditorGUILayout.Foldout(showBase, "Base", true);
        if (showBase)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(questId);
                    if (GUILayout.Button("From Asset Name", GUILayout.Width(120f)))
                    {
                        questId.stringValue = QuestToolsEditorUtility.SanitizeId(selectedQuest.name, "quest");
                    }
                }

                EditorGUILayout.PropertyField(questType);
                EditorGUILayout.PropertyField(startType);
                EditorGUILayout.PropertyField(repeatMode);
            }
        }

        showFlags = EditorGUILayout.Foldout(showFlags, "Flags", true);
        if (showFlags)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(hiddenUntilStarted);
                EditorGUILayout.PropertyField(canRestartAfterFail);
                EditorGUILayout.PropertyField(notifyOnAccept);
                EditorGUILayout.PropertyField(notifyOnComplete);
            }
        }

        showTexts = EditorGUILayout.Foldout(showTexts, "Texts", true);
        if (showTexts)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(questTitle, true);
                EditorGUILayout.PropertyField(shortDescription, true);
                EditorGUILayout.PropertyField(fullDescription, true);
            }
        }

        showAvailability = EditorGUILayout.Foldout(showAvailability, "Availability Conditions", true);
        if (showAvailability)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(requiredPlayerLevel);
                EditorGUILayout.PropertyField(requiredItemId);
                EditorGUILayout.PropertyField(requiredCompletedQuestId);
            }
        }

        showSteps = EditorGUILayout.Foldout(showSteps, $"Steps ({steps.arraySize})", true);
        if (showSteps)
        {
            DrawStepsSection(so, steps);
        }
    }

    private void DrawStepsSection(SerializedObject so, SerializedProperty steps)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Step"))
                {
                    Undo.RecordObject(selectedQuest, "Add Quest Step");
                    QuestToolsEditorUtility.AddStep(steps, so.FindProperty("questId").stringValue);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Generate IDs"))
                {
                    Undo.RecordObject(selectedQuest, "Generate Quest IDs");
                    QuestToolsEditorUtility.GenerateIds(so);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(4f);

            if (steps.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This quest has no steps.", MessageType.Warning);
            }

            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(i);
                DrawSingleStep(so, steps, step, i);
                EditorGUILayout.Space(6f);
            }
        }
    }

    private void DrawSingleStep(SerializedObject so, SerializedProperty steps, SerializedProperty step, int stepIndex)
    {
        SerializedProperty stepId = step.FindPropertyRelative("stepId");
        SerializedProperty stepDescription = step.FindPropertyRelative("stepDescription");
        SerializedProperty stepTaskText = step.FindPropertyRelative("stepTaskText");
        SerializedProperty objectives = step.FindPropertyRelative("objectives");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string title = string.IsNullOrWhiteSpace(stepId.stringValue)
                    ? $"Step {stepIndex + 1}"
                    : $"Step {stepIndex + 1}: {stepId.stringValue}";

                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                GUI.enabled = stepIndex > 0;
                if (GUILayout.Button("▲", GUILayout.Width(28f)))
                {
                    Undo.RecordObject(selectedQuest, "Move Quest Step");
                    steps.MoveArrayElement(stepIndex, stepIndex - 1);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = stepIndex < steps.arraySize - 1;
                if (GUILayout.Button("▼", GUILayout.Width(28f)))
                {
                    Undo.RecordObject(selectedQuest, "Move Quest Step");
                    steps.MoveArrayElement(stepIndex, stepIndex + 1);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;

                if (GUILayout.Button("Clone", GUILayout.Width(52f)))
                {
                    Undo.RecordObject(selectedQuest, "Duplicate Quest Step");
                    QuestToolsEditorUtility.DuplicateStep(so.FindProperty("steps"), stepIndex);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Delete", GUILayout.Width(56f)))
                {
                    if (EditorUtility.DisplayDialog("Delete step?", $"Delete step '{stepId.stringValue}'?", "Delete", "Cancel"))
                    {
                        Undo.RecordObject(selectedQuest, "Delete Quest Step");
                        steps.DeleteArrayElementAtIndex(stepIndex);
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(selectedQuest);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(stepId);
                if (GUILayout.Button("Auto", GUILayout.Width(60f)))
                {
                    stepId.stringValue = QuestToolsEditorUtility.GetRecommendedStepId(so.FindProperty("questId").stringValue, stepIndex);
                }
            }

            EditorGUILayout.PropertyField(stepDescription, true);
            EditorGUILayout.PropertyField(stepTaskText, true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Objectives ({objectives.arraySize})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Objective"))
                {
                    Undo.RecordObject(selectedQuest, "Add Quest Objective");
                    QuestToolsEditorUtility.AddObjective(so.FindProperty("steps"), stepIndex);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }
            }

            if (objectives.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This step has no objectives. Runtime code will auto-advance zero-objective steps.", MessageType.Warning);
            }

            for (int j = 0; j < objectives.arraySize; j++)
            {
                SerializedProperty objective = objectives.GetArrayElementAtIndex(j);
                DrawSingleObjective(so, objectives, objective, stepIndex, j);
                EditorGUILayout.Space(4f);
            }
        }
    }

    private void DrawSingleObjective(SerializedObject so, SerializedProperty objectives, SerializedProperty objective, int stepIndex, int objectiveIndex)
    {
        SerializedProperty objectiveId = objective.FindPropertyRelative("objectiveId");
        SerializedProperty objectiveType = objective.FindPropertyRelative("objectiveType");
        SerializedProperty objectiveDescription = objective.FindPropertyRelative("objectiveDescription");
        SerializedProperty targetId = objective.FindPropertyRelative("targetId");
        SerializedProperty requiredAmount = objective.FindPropertyRelative("requiredAmount");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string title = string.IsNullOrWhiteSpace(objectiveId.stringValue)
                    ? $"Objective {objectiveIndex + 1}"
                    : $"Objective {objectiveIndex + 1}: {objectiveId.stringValue}";

                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                GUI.enabled = objectiveIndex > 0;
                if (GUILayout.Button("▲", GUILayout.Width(28f)))
                {
                    Undo.RecordObject(selectedQuest, "Move Quest Objective");
                    objectives.MoveArrayElement(objectiveIndex, objectiveIndex - 1);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = objectiveIndex < objectives.arraySize - 1;
                if (GUILayout.Button("▼", GUILayout.Width(28f)))
                {
                    Undo.RecordObject(selectedQuest, "Move Quest Objective");
                    objectives.MoveArrayElement(objectiveIndex, objectiveIndex + 1);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;

                if (GUILayout.Button("Clone", GUILayout.Width(52f)))
                {
                    Undo.RecordObject(selectedQuest, "Duplicate Quest Objective");
                    QuestToolsEditorUtility.DuplicateObjective(so.FindProperty("steps"), stepIndex, objectiveIndex);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Delete", GUILayout.Width(56f)))
                {
                    Undo.RecordObject(selectedQuest, "Delete Quest Objective");
                    objectives.DeleteArrayElementAtIndex(objectiveIndex);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectedQuest);
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(objectiveId);
                if (GUILayout.Button("Auto", GUILayout.Width(60f)))
                {
                    string stepId = so.FindProperty("steps").GetArrayElementAtIndex(stepIndex).FindPropertyRelative("stepId").stringValue;
                    objectiveId.stringValue = QuestToolsEditorUtility.GetRecommendedObjectiveId(stepId, objectiveIndex);
                }
            }

            EditorGUILayout.PropertyField(objectiveType);
            EditorGUILayout.PropertyField(objectiveDescription, true);
            EditorGUILayout.PropertyField(targetId);
            EditorGUILayout.PropertyField(requiredAmount);

            QuestObjectiveType type = (QuestObjectiveType)objectiveType.enumValueIndex;
            string hint = QuestToolsEditorUtility.GetObjectiveTargetHint(type);
            if (!string.IsNullOrWhiteSpace(hint))
            {
                EditorGUILayout.HelpBox(hint, MessageType.Info);
            }
        }
    }
}

internal static class QuestToolsEditorUtility
{
    internal static List<QuestData> FindAllQuestAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:QuestData");
        List<QuestData> quests = new List<QuestData>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (quest != null)
            {
                quests.Add(quest);
            }
        }

        quests.Sort((a, b) => string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), StringComparison.OrdinalIgnoreCase));
        return quests;
    }

    internal static QuestData CreateQuestAsset()
    {
        string folder = GetBestCreateFolder();
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "QuestData_NewQuest.asset").Replace("\\", "/"));

        QuestData asset = ScriptableObject.CreateInstance<QuestData>();
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GenerateIds(asset);

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        return asset;
    }

    internal static void GenerateIds(QuestData quest)
    {
        if (quest == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(quest);
        so.Update();
        GenerateIds(so);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(quest);
        AssetDatabase.SaveAssets();
    }

    internal static void GenerateIds(SerializedObject so)
    {
        if (so == null)
        {
            return;
        }

        SerializedProperty questIdProp = so.FindProperty("questId");
        SerializedProperty stepsProp = so.FindProperty("steps");

        string questId = SanitizeId(questIdProp != null ? questIdProp.stringValue : string.Empty, "quest");
        if (string.IsNullOrWhiteSpace(questId))
        {
            UnityEngine.Object target = so.targetObject;
            questId = SanitizeId(target != null ? target.name : "quest", "quest");
        }

        if (questIdProp != null)
        {
            questIdProp.stringValue = questId;
        }

        if (stepsProp == null)
        {
            return;
        }

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            SerializedProperty step = stepsProp.GetArrayElementAtIndex(i);
            SerializedProperty stepIdProp = step.FindPropertyRelative("stepId");
            SerializedProperty objectivesProp = step.FindPropertyRelative("objectives");

            string stepId = GetRecommendedStepId(questId, i);
            if (stepIdProp != null)
            {
                stepIdProp.stringValue = stepId;
            }

            if (objectivesProp == null)
            {
                continue;
            }

            for (int j = 0; j < objectivesProp.arraySize; j++)
            {
                SerializedProperty objective = objectivesProp.GetArrayElementAtIndex(j);
                SerializedProperty objectiveIdProp = objective.FindPropertyRelative("objectiveId");
                SerializedProperty requiredAmountProp = objective.FindPropertyRelative("requiredAmount");

                if (objectiveIdProp != null)
                {
                    objectiveIdProp.stringValue = GetRecommendedObjectiveId(stepId, j);
                }

                if (requiredAmountProp != null && requiredAmountProp.intValue < 1)
                {
                    requiredAmountProp.intValue = 1;
                }
            }
        }
    }

    internal static string GetRecommendedStepId(string questId, int stepIndex)
    {
        string safeQuestId = SanitizeId(questId, "quest");
        return $"{safeQuestId}_step_{stepIndex + 1:00}";
    }

    internal static string GetRecommendedObjectiveId(string stepId, int objectiveIndex)
    {
        string safeStepId = SanitizeId(stepId, "step");
        return $"{safeStepId}_objective_{objectiveIndex + 1:00}";
    }

    internal static void AddStep(SerializedProperty stepsProp, string questId)
    {
        if (stepsProp == null)
        {
            return;
        }

        int newIndex = stepsProp.arraySize;
        stepsProp.InsertArrayElementAtIndex(newIndex);

        SerializedProperty step = stepsProp.GetArrayElementAtIndex(newIndex);
        ResetStep(step, GetRecommendedStepId(questId, newIndex));
    }

    internal static void DuplicateStep(SerializedProperty stepsProp, int sourceIndex)
    {
        if (stepsProp == null || sourceIndex < 0 || sourceIndex >= stepsProp.arraySize)
        {
            return;
        }

        stepsProp.InsertArrayElementAtIndex(sourceIndex + 1);
        SerializedProperty duplicated = stepsProp.GetArrayElementAtIndex(sourceIndex + 1);

        SerializedProperty stepIdProp = duplicated.FindPropertyRelative("stepId");
        string newStepId = MakeUniqueStepId(stepsProp, sourceIndex + 1, SanitizeId(stepIdProp.stringValue + "_copy", "step"));
        stepIdProp.stringValue = newStepId;

        SerializedProperty objectives = duplicated.FindPropertyRelative("objectives");
        if (objectives != null)
        {
            for (int j = 0; j < objectives.arraySize; j++)
            {
                SerializedProperty objective = objectives.GetArrayElementAtIndex(j);
                SerializedProperty objectiveId = objective.FindPropertyRelative("objectiveId");
                if (objectiveId != null)
                {
                    objectiveId.stringValue = MakeUniqueObjectiveId(stepsProp, sourceIndex + 1, j, GetRecommendedObjectiveId(newStepId, j));
                }
            }
        }
    }

    internal static void AddObjective(SerializedProperty stepsProp, int stepIndex)
    {
        if (stepsProp == null || stepIndex < 0 || stepIndex >= stepsProp.arraySize)
        {
            return;
        }

        SerializedProperty step = stepsProp.GetArrayElementAtIndex(stepIndex);
        SerializedProperty objectives = step.FindPropertyRelative("objectives");
        SerializedProperty stepId = step.FindPropertyRelative("stepId");

        int newIndex = objectives.arraySize;
        objectives.InsertArrayElementAtIndex(newIndex);

        SerializedProperty objective = objectives.GetArrayElementAtIndex(newIndex);
        ResetObjective(objective, GetRecommendedObjectiveId(stepId.stringValue, newIndex));
    }

    internal static void DuplicateObjective(SerializedProperty stepsProp, int stepIndex, int objectiveIndex)
    {
        if (stepsProp == null || stepIndex < 0 || stepIndex >= stepsProp.arraySize)
        {
            return;
        }

        SerializedProperty step = stepsProp.GetArrayElementAtIndex(stepIndex);
        SerializedProperty objectives = step.FindPropertyRelative("objectives");
        if (objectives == null || objectiveIndex < 0 || objectiveIndex >= objectives.arraySize)
        {
            return;
        }

        objectives.InsertArrayElementAtIndex(objectiveIndex + 1);
        SerializedProperty duplicated = objectives.GetArrayElementAtIndex(objectiveIndex + 1);

        SerializedProperty stepIdProp = step.FindPropertyRelative("stepId");
        SerializedProperty objectiveIdProp = duplicated.FindPropertyRelative("objectiveId");
        string baseId = GetRecommendedObjectiveId(stepIdProp.stringValue, objectiveIndex + 1);

        if (objectiveIdProp != null)
        {
            objectiveIdProp.stringValue = MakeUniqueObjectiveId(stepsProp, stepIndex, objectiveIndex + 1, baseId);
        }

        SerializedProperty requiredAmount = duplicated.FindPropertyRelative("requiredAmount");
        if (requiredAmount != null && requiredAmount.intValue < 1)
        {
            requiredAmount.intValue = 1;
        }
    }

    internal static int SyncAllQuestManagersInOpenScenes()
    {
        QuestManager[] managers = Resources.FindObjectsOfTypeAll<QuestManager>();
        int syncedCount = 0;

        for (int i = 0; i < managers.Length; i++)
        {
            QuestManager manager = managers[i];
            if (manager == null)
            {
                continue;
            }

            if (!manager.gameObject.scene.IsValid())
            {
                continue;
            }

            if (EditorUtility.IsPersistent(manager))
            {
                continue;
            }

            SyncQuestManager(manager);
            syncedCount++;
        }

        return syncedCount;
    }

    internal static void SyncQuestManager(QuestManager manager)
    {
        if (manager == null)
        {
            return;
        }

        List<QuestData> allQuests = FindAllQuestAssets();
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty allQuestDataProp = so.FindProperty("allQuestData");

        if (allQuestDataProp == null)
        {
            Debug.LogWarning("Quest Tools: could not find serialized field 'allQuestData' on QuestManager.");
            return;
        }

        so.Update();
        allQuestDataProp.arraySize = allQuests.Count;
        for (int i = 0; i < allQuests.Count; i++)
        {
            allQuestDataProp.GetArrayElementAtIndex(i).objectReferenceValue = allQuests[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        if (manager.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }

    internal static List<QuestValidationMessage> ValidateQuest(QuestData quest)
    {
        List<QuestValidationMessage> messages = new List<QuestValidationMessage>();

        if (quest == null)
        {
            messages.Add(new QuestValidationMessage(MessageType.Error, "Quest asset is null."));
            return messages;
        }

        SerializedObject so = new SerializedObject(quest);
        so.Update();

        SerializedProperty questId = so.FindProperty("questId");
        SerializedProperty startType = so.FindProperty("startType");
        SerializedProperty requiredPlayerLevel = so.FindProperty("requiredPlayerLevel");
        SerializedProperty requiredItemId = so.FindProperty("requiredItemId");
        SerializedProperty requiredCompletedQuestId = so.FindProperty("requiredCompletedQuestId");
        SerializedProperty questTitle = so.FindProperty("questTitle");
        SerializedProperty shortDescription = so.FindProperty("shortDescription");
        SerializedProperty fullDescription = so.FindProperty("fullDescription");
        SerializedProperty steps = so.FindProperty("steps");

        string questIdValue = questId != null ? questId.stringValue : string.Empty;

        if (string.IsNullOrWhiteSpace(questIdValue))
        {
            messages.Add(new QuestValidationMessage(MessageType.Error, "QuestId is empty."));
        }
        else
        {
            List<QuestData> allQuests = FindAllQuestAssets();
            for (int i = 0; i < allQuests.Count; i++)
            {
                if (allQuests[i] == null || allQuests[i] == quest)
                {
                    continue;
                }

                if (string.Equals(allQuests[i].QuestId, questIdValue, StringComparison.Ordinal))
                {
                    messages.Add(new QuestValidationMessage(MessageType.Error, $"Duplicate QuestId found in project: '{questIdValue}'."));
                    break;
                }
            }
        }

        if (requiredCompletedQuestId != null && string.Equals(requiredCompletedQuestId.stringValue, questIdValue, StringComparison.Ordinal))
        {
            messages.Add(new QuestValidationMessage(MessageType.Error, "requiredCompletedQuestId cannot reference the same quest."));
        }

        if (!IsLocalizedStringAssigned(questTitle))
        {
            messages.Add(new QuestValidationMessage(MessageType.Warning, "Quest Title LocalizedString is not assigned."));
        }

        if (!IsLocalizedStringAssigned(shortDescription))
        {
            messages.Add(new QuestValidationMessage(MessageType.Warning, "Short Description LocalizedString is not assigned."));
        }

        if (!IsLocalizedStringAssigned(fullDescription))
        {
            messages.Add(new QuestValidationMessage(MessageType.Warning, "Full Description LocalizedString is not assigned."));
        }

        if (startType != null)
        {
            QuestStartType startTypeValue = (QuestStartType)startType.enumValueIndex;

            if (startTypeValue == QuestStartType.ItemReceived && (requiredItemId == null || string.IsNullOrWhiteSpace(requiredItemId.stringValue)))
            {
                messages.Add(new QuestValidationMessage(MessageType.Error, "StartType = ItemReceived, but requiredItemId is empty."));
            }

            if (startTypeValue == QuestStartType.QuestCompleted && (requiredCompletedQuestId == null || string.IsNullOrWhiteSpace(requiredCompletedQuestId.stringValue)))
            {
                messages.Add(new QuestValidationMessage(MessageType.Error, "StartType = QuestCompleted, but requiredCompletedQuestId is empty."));
            }

            if (startTypeValue == QuestStartType.PlayerLevelReached && requiredPlayerLevel != null && requiredPlayerLevel.intValue <= 0)
            {
                messages.Add(new QuestValidationMessage(MessageType.Warning, "StartType = PlayerLevelReached, but requiredPlayerLevel is 0 or less."));
            }
        }

        if (steps == null || steps.arraySize == 0)
        {
            messages.Add(new QuestValidationMessage(MessageType.Warning, "Quest has no steps."));
            return messages;
        }

        HashSet<string> usedStepIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> usedObjectiveIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            SerializedProperty stepId = step.FindPropertyRelative("stepId");
            SerializedProperty stepDescription = step.FindPropertyRelative("stepDescription");
            SerializedProperty stepTaskText = step.FindPropertyRelative("stepTaskText");
            SerializedProperty objectives = step.FindPropertyRelative("objectives");

            string stepIdValue = stepId != null ? stepId.stringValue : string.Empty;

            if (string.IsNullOrWhiteSpace(stepIdValue))
            {
                messages.Add(new QuestValidationMessage(MessageType.Error, $"Step {i + 1}: stepId is empty."));
            }
            else if (!usedStepIds.Add(stepIdValue))
            {
                messages.Add(new QuestValidationMessage(MessageType.Error, $"Duplicate stepId in quest: '{stepIdValue}'."));
            }

            if (!IsLocalizedStringAssigned(stepDescription))
            {
                messages.Add(new QuestValidationMessage(MessageType.Warning, $"Step {i + 1}: stepDescription LocalizedString is not assigned."));
            }

            if (!IsLocalizedStringAssigned(stepTaskText))
            {
                messages.Add(new QuestValidationMessage(MessageType.Warning, $"Step {i + 1}: stepTaskText LocalizedString is not assigned."));
            }

            if (objectives == null || objectives.arraySize == 0)
            {
                messages.Add(new QuestValidationMessage(MessageType.Warning, $"Step {i + 1} has no objectives. Runtime will auto-advance this step."));
                continue;
            }

            for (int j = 0; j < objectives.arraySize; j++)
            {
                SerializedProperty objective = objectives.GetArrayElementAtIndex(j);
                SerializedProperty objectiveId = objective.FindPropertyRelative("objectiveId");
                SerializedProperty objectiveType = objective.FindPropertyRelative("objectiveType");
                SerializedProperty objectiveDescription = objective.FindPropertyRelative("objectiveDescription");
                SerializedProperty targetId = objective.FindPropertyRelative("targetId");
                SerializedProperty requiredAmount = objective.FindPropertyRelative("requiredAmount");

                string objectiveIdValue = objectiveId != null ? objectiveId.stringValue : string.Empty;
                string targetIdValue = targetId != null ? targetId.stringValue : string.Empty;
                int amount = requiredAmount != null ? requiredAmount.intValue : 0;

                if (string.IsNullOrWhiteSpace(objectiveIdValue))
                {
                    messages.Add(new QuestValidationMessage(MessageType.Error, $"Step {i + 1}, Objective {j + 1}: objectiveId is empty."));
                }
                else if (!usedObjectiveIds.Add(objectiveIdValue))
                {
                    messages.Add(new QuestValidationMessage(MessageType.Error, $"Duplicate objectiveId in quest: '{objectiveIdValue}'."));
                }

                if (!IsLocalizedStringAssigned(objectiveDescription))
                {
                    messages.Add(new QuestValidationMessage(MessageType.Warning, $"Objective '{objectiveIdValue}': objectiveDescription LocalizedString is not assigned."));
                }

                if (amount <= 0)
                {
                    messages.Add(new QuestValidationMessage(MessageType.Error, $"Objective '{objectiveIdValue}': requiredAmount must be greater than 0."));
                }

                if (objectiveType != null)
                {
                    QuestObjectiveType type = (QuestObjectiveType)objectiveType.enumValueIndex;
                    bool targetIsRequired = true;

                    if (targetIsRequired && string.IsNullOrWhiteSpace(targetIdValue))
                    {
                        messages.Add(new QuestValidationMessage(MessageType.Error, $"Objective '{objectiveIdValue}': targetId is empty."));
                    }

                    if (type == QuestObjectiveType.CompleteQuest && string.Equals(targetIdValue, questIdValue, StringComparison.Ordinal))
                    {
                        messages.Add(new QuestValidationMessage(MessageType.Warning, $"Objective '{objectiveIdValue}': targetId points to the same quest. Check if this is intentional."));
                    }
                }
            }
        }

        return messages;
    }

    internal static void LogValidationToConsole(QuestData quest)
    {
        List<QuestValidationMessage> messages = ValidateQuest(quest);
        string assetPath = quest != null ? AssetDatabase.GetAssetPath(quest) : "<null>";

        if (messages.Count == 0)
        {
            Debug.Log($"Quest Validation OK: {assetPath}");
            return;
        }

        string combined = string.Join("\n", messages.Select(m => $"- [{m.Type}] {m.Text}").ToArray());
        bool hasError = messages.Any(m => m.Type == MessageType.Error);

        if (hasError)
        {
            Debug.LogError($"Quest Validation FAILED: {assetPath}\n{combined}", quest);
        }
        else
        {
            Debug.LogWarning($"Quest Validation WARNINGS: {assetPath}\n{combined}", quest);
        }
    }

    internal static void LogValidationForAllQuests()
    {
        List<QuestData> quests = FindAllQuestAssets();
        int totalIssues = 0;
        int questsWithErrors = 0;

        for (int i = 0; i < quests.Count; i++)
        {
            List<QuestValidationMessage> messages = ValidateQuest(quests[i]);
            if (messages.Count == 0)
            {
                continue;
            }

            totalIssues += messages.Count;
            if (messages.Any(m => m.Type == MessageType.Error))
            {
                questsWithErrors++;
            }

            LogValidationToConsole(quests[i]);
        }

        if (totalIssues == 0)
        {
            Debug.Log($"Quest Validation: checked {quests.Count} quest(s). No issues found.");
        }
        else
        {
            Debug.LogWarning($"Quest Validation: checked {quests.Count} quest(s). Total issues: {totalIssues}. Quests with errors: {questsWithErrors}.");
        }
    }

    internal static string SanitizeId(string raw, string fallback)
    {
        string value = string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9]+", "_");
        value = Regex.Replace(value, @"_+", "_").Trim('_');

        if (string.IsNullOrWhiteSpace(value))
        {
            value = fallback;
        }

        return value;
    }

    internal static string GetObjectiveTargetHint(QuestObjectiveType type)
    {
        switch (type)
        {
            case QuestObjectiveType.ReachTriggerZone:
                return "targetId должен совпадать с QuestTriggerZone.zoneId.";
            case QuestObjectiveType.TalkToNpc:
                return "targetId должен совпадать с NPC id, который ты передаёшь в NotifyNpcTalked(...).";
            case QuestObjectiveType.KillEnemyType:
                return "targetId должен совпадать с enemyTypeId, который ты передаёшь в NotifyEnemyKilled(enemyTypeId, enemyUniqueId).";
            case QuestObjectiveType.KillSpecificEnemy:
                return "targetId должен совпадать с enemyUniqueId, который ты передаёшь в NotifyEnemyKilled(enemyTypeId, enemyUniqueId).";
            case QuestObjectiveType.ObtainItem:
                return "targetId должен совпадать с ItemId предмета.";
            case QuestObjectiveType.CompleteQuest:
                return "targetId должен совпадать с QuestId другого квеста.";
            default:
                return string.Empty;
        }
    }

    private static string GetBestCreateFolder()
    {
        UnityEngine.Object active = Selection.activeObject;
        if (active == null)
        {
            EnsureFolderExists("Assets/Quests");
            return "Assets/Quests";
        }

        string path = AssetDatabase.GetAssetPath(active);
        if (string.IsNullOrWhiteSpace(path))
        {
            EnsureFolderExists("Assets/Quests");
            return "Assets/Quests";
        }

        if (AssetDatabase.IsValidFolder(path))
        {
            return path;
        }

        string directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            EnsureFolderExists("Assets/Quests");
            return "Assets/Quests";
        }

        return directory.Replace("\\", "/");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void ResetStep(SerializedProperty step, string defaultStepId)
    {
        if (step == null)
        {
            return;
        }

        SerializedProperty stepId = step.FindPropertyRelative("stepId");
        SerializedProperty stepDescription = step.FindPropertyRelative("stepDescription");
        SerializedProperty stepTaskText = step.FindPropertyRelative("stepTaskText");
        SerializedProperty objectives = step.FindPropertyRelative("objectives");

        if (stepId != null)
        {
            stepId.stringValue = defaultStepId;
        }

        ResetLocalizedString(stepDescription);
        ResetLocalizedString(stepTaskText);

        if (objectives != null)
        {
            while (objectives.arraySize > 0)
            {
                objectives.DeleteArrayElementAtIndex(objectives.arraySize - 1);
            }

            objectives.InsertArrayElementAtIndex(0);
            ResetObjective(objectives.GetArrayElementAtIndex(0), GetRecommendedObjectiveId(defaultStepId, 0));
        }
    }

    private static void ResetObjective(SerializedProperty objective, string defaultObjectiveId)
    {
        if (objective == null)
        {
            return;
        }

        SerializedProperty objectiveId = objective.FindPropertyRelative("objectiveId");
        SerializedProperty objectiveType = objective.FindPropertyRelative("objectiveType");
        SerializedProperty objectiveDescription = objective.FindPropertyRelative("objectiveDescription");
        SerializedProperty targetId = objective.FindPropertyRelative("targetId");
        SerializedProperty requiredAmount = objective.FindPropertyRelative("requiredAmount");

        if (objectiveId != null)
        {
            objectiveId.stringValue = defaultObjectiveId;
        }

        if (objectiveType != null)
        {
            objectiveType.enumValueIndex = 0;
        }

        ResetLocalizedString(objectiveDescription);

        if (targetId != null)
        {
            targetId.stringValue = string.Empty;
        }

        if (requiredAmount != null)
        {
            requiredAmount.intValue = 1;
        }
    }

    private static void ResetLocalizedString(SerializedProperty localizedStringProperty)
    {
        if (localizedStringProperty == null)
        {
            return;
        }

        SerializedProperty tableReference = localizedStringProperty.FindPropertyRelative("m_TableReference");
        SerializedProperty tableName = tableReference != null ? tableReference.FindPropertyRelative("m_TableCollectionName") : null;

        SerializedProperty entryReference = localizedStringProperty.FindPropertyRelative("m_TableEntryReference");
        SerializedProperty keyId = entryReference != null ? entryReference.FindPropertyRelative("m_KeyId") : null;
        SerializedProperty key = entryReference != null ? entryReference.FindPropertyRelative("m_Key") : null;

        SerializedProperty localVariables = localizedStringProperty.FindPropertyRelative("m_LocalVariables");

        if (tableName != null)
        {
            tableName.stringValue = string.Empty;
        }

        if (keyId != null)
        {
            keyId.longValue = 0;
        }

        if (key != null)
        {
            key.stringValue = string.Empty;
        }

        if (localVariables != null)
        {
            localVariables.arraySize = 0;
        }
    }

    private static bool IsLocalizedStringAssigned(SerializedProperty localizedStringProperty)
    {
        if (localizedStringProperty == null)
        {
            return false;
        }

        SerializedProperty tableReference = localizedStringProperty.FindPropertyRelative("m_TableReference");
        SerializedProperty tableName = tableReference != null ? tableReference.FindPropertyRelative("m_TableCollectionName") : null;

        SerializedProperty entryReference = localizedStringProperty.FindPropertyRelative("m_TableEntryReference");
        SerializedProperty keyId = entryReference != null ? entryReference.FindPropertyRelative("m_KeyId") : null;
        SerializedProperty key = entryReference != null ? entryReference.FindPropertyRelative("m_Key") : null;

        bool hasTable = tableName != null && !string.IsNullOrWhiteSpace(tableName.stringValue);
        bool hasKeyId = keyId != null && keyId.longValue != 0;
        bool hasKeyString = key != null && !string.IsNullOrWhiteSpace(key.stringValue);

        return hasTable && (hasKeyId || hasKeyString);
    }

    private static string MakeUniqueStepId(SerializedProperty stepsProp, int currentIndex, string baseId)
    {
        string safe = SanitizeId(baseId, "step");
        HashSet<string> existing = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            if (i == currentIndex)
            {
                continue;
            }

            SerializedProperty step = stepsProp.GetArrayElementAtIndex(i);
            SerializedProperty stepId = step.FindPropertyRelative("stepId");
            if (stepId != null && !string.IsNullOrWhiteSpace(stepId.stringValue))
            {
                existing.Add(stepId.stringValue);
            }
        }

        if (!existing.Contains(safe))
        {
            return safe;
        }

        int suffix = 2;
        while (existing.Contains($"{safe}_{suffix}"))
        {
            suffix++;
        }

        return $"{safe}_{suffix}";
    }

    private static string MakeUniqueObjectiveId(SerializedProperty stepsProp, int currentStepIndex, int currentObjectiveIndex, string baseId)
    {
        string safe = SanitizeId(baseId, "objective");
        HashSet<string> existing = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            SerializedProperty step = stepsProp.GetArrayElementAtIndex(i);
            SerializedProperty objectives = step.FindPropertyRelative("objectives");
            if (objectives == null)
            {
                continue;
            }

            for (int j = 0; j < objectives.arraySize; j++)
            {
                if (i == currentStepIndex && j == currentObjectiveIndex)
                {
                    continue;
                }

                SerializedProperty objective = objectives.GetArrayElementAtIndex(j);
                SerializedProperty objectiveId = objective.FindPropertyRelative("objectiveId");
                if (objectiveId != null && !string.IsNullOrWhiteSpace(objectiveId.stringValue))
                {
                    existing.Add(objectiveId.stringValue);
                }
            }
        }

        if (!existing.Contains(safe))
        {
            return safe;
        }

        int suffix = 2;
        while (existing.Contains($"{safe}_{suffix}"))
        {
            suffix++;
        }

        return $"{safe}_{suffix}";
    }
}

internal sealed class QuestValidationMessage
{
    public MessageType Type { get; private set; }
    public string Text { get; private set; }

    public QuestValidationMessage(MessageType type, string text)
    {
        Type = type;
        Text = text;
    }
}
#endif