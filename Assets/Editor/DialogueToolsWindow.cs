#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class DialogueToolsWindow : EditorWindow
{
    private enum EditorTab
    {
        Dialogues = 0,
        Speakers = 1
    }

    private enum MessageKind
    {
        Info,
        Warning,
        Error
    }

    private sealed class ValidationMessage
    {
        public MessageKind Kind;
        public string Text;

        public ValidationMessage(MessageKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }

    private static class Styles
    {
        public static GUIStyle ToolbarTitle;
        public static GUIStyle BigTitle;
        public static GUIStyle MiniBadge;
        public static GUIStyle WrappedMiniLabel;
        public static GUIStyle SectionTitle;

        public static bool TryEnsure()
        {
            if (EditorStyles.label == null || EditorStyles.boldLabel == null || EditorStyles.miniLabel == null || EditorStyles.miniBoldLabel == null)
                return false;

            if (ToolbarTitle == null)
            {
                ToolbarTitle = new GUIStyle(EditorStyles.boldLabel);
                ToolbarTitle.alignment = TextAnchor.MiddleLeft;
            }

            if (BigTitle == null)
            {
                BigTitle = new GUIStyle(EditorStyles.boldLabel);
                BigTitle.fontSize = 14;
            }

            if (MiniBadge == null)
            {
                MiniBadge = new GUIStyle(EditorStyles.miniBoldLabel);
                MiniBadge.alignment = TextAnchor.MiddleCenter;
                MiniBadge.padding = new RectOffset(6, 6, 2, 2);
            }

            if (WrappedMiniLabel == null)
            {
                WrappedMiniLabel = new GUIStyle(EditorStyles.miniLabel);
                WrappedMiniLabel.wordWrap = true;
            }

            if (SectionTitle == null)
            {
                SectionTitle = new GUIStyle(EditorStyles.boldLabel);
                SectionTitle.fontSize = 12;
            }

            return true;
        }

        public static void Reset()
        {
            ToolbarTitle = null;
            BigTitle = null;
            MiniBadge = null;
            WrappedMiniLabel = null;
            SectionTitle = null;
        }
    }

    private const string DialogueFolder = "Assets/Dialogue/Data";
    private const string SpeakerFolder = "Assets/Dialogue/DataSpeakers";
    private const float LeftPanelWidth = 290f;
    private const float MiddlePanelWidth = 360f;
    private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private EditorTab currentTab = EditorTab.Dialogues;
    private string searchText = string.Empty;

    private Vector2 leftScroll;
    private Vector2 middleScroll;
    private Vector2 rightScroll;

    private List<DialogueData> dialogueAssets = new List<DialogueData>();
    private List<DialogueSpeakerData> speakerAssets = new List<DialogueSpeakerData>();

    private DialogueData selectedDialogue;
    private DialogueSpeakerData selectedSpeaker;

    private SerializedObject selectedDialogueSO;
    private SerializedObject selectedSpeakerSO;

    private int selectedNodeIndex = -1;
    private bool showValidationFoldout = true;

    [MenuItem("Tools/HeroTest/Dialogue/Open Editor")]
    public static void OpenWindow()
    {
        DialogueToolsWindow window = GetWindow<DialogueToolsWindow>("Dialogue Tools");
        window.minSize = new Vector2(1280f, 760f);
        window.Show();
        window.Focus();
    }

    [MenuItem("Tools/HeroTest/Dialogue/Create/New Dialogue Data")]
    public static void CreateDialogueMenuItem()
    {
        DialogueData asset = CreateDialogueAsset();
        OpenAndSelectDialogue(asset);
    }

    [MenuItem("Tools/HeroTest/Dialogue/Create/New Speaker Data")]
    public static void CreateSpeakerMenuItem()
    {
        DialogueSpeakerData asset = CreateSpeakerAsset();
        OpenAndSelectSpeaker(asset);
    }

    [MenuItem("Tools/HeroTest/Dialogue/Validate Selected Dialogue", true)]
    private static bool CanValidateSelectedDialogue()
    {
        return Selection.activeObject is DialogueData;
    }

    [MenuItem("Tools/HeroTest/Dialogue/Validate Selected Dialogue")]
    private static void ValidateSelectedDialogue()
    {
        DialogueData dialogue = Selection.activeObject as DialogueData;
        if (dialogue == null)
            return;

        dialogue.ValidateDialogue();
        EditorUtility.SetDirty(dialogue);
        AssetDatabase.SaveAssets();
        Debug.Log("[Dialogue Tools] Validation completed for '" + dialogue.name + "'.", dialogue);
    }

    public static void OpenAndSelectDialogue(DialogueData dialogue)
    {
        DialogueToolsWindow window = GetWindow<DialogueToolsWindow>("Dialogue Tools");
        window.minSize = new Vector2(1280f, 760f);
        window.currentTab = EditorTab.Dialogues;
        window.RefreshAssetLists();
        window.SelectDialogue(dialogue);
        window.Show();
        window.Focus();
    }

    public static void OpenAndSelectSpeaker(DialogueSpeakerData speaker)
    {
        DialogueToolsWindow window = GetWindow<DialogueToolsWindow>("Dialogue Tools");
        window.minSize = new Vector2(1280f, 760f);
        window.currentTab = EditorTab.Speakers;
        window.RefreshAssetLists();
        window.SelectSpeaker(speaker);
        window.Show();
        window.Focus();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Dialogue Tools");
        minSize = new Vector2(1280f, 760f);

        RefreshAssetLists();
        TryAdoptCurrentSelection();

        if (selectedDialogue == null && dialogueAssets.Count > 0)
            SelectDialogue(dialogueAssets[0]);

        if (selectedSpeaker == null && speakerAssets.Count > 0)
            SelectSpeaker(speakerAssets[0]);

        Undo.undoRedoPerformed += HandleUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;
        Styles.Reset();
    }

    private void OnFocus()
    {
        RefreshAssetLists();
    }

    private void OnSelectionChange()
    {
        TryAdoptCurrentSelection();
        Repaint();
    }

    private void HandleUndoRedo()
    {
        RebuildSerializedObjects();
        Repaint();
    }

    private void OnGUI()
    {
        if (!Styles.TryEnsure())
        {
            Repaint();
            return;
        }

        DrawToolbar();

        EditorGUILayout.BeginHorizontal();

        DrawLeftPanel();
        DrawMiddlePanel();
        DrawRightPanel();

        EditorGUILayout.EndHorizontal();

        ApplyPendingSerializedChanges();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            currentTab = (EditorTab)GUILayout.Toolbar((int)currentTab, new[] { "Dialogues", "Speakers" }, EditorStyles.toolbarButton, GUILayout.Width(180f));

            GUILayout.Space(10f);
            GUILayout.Label("Search", Styles.ToolbarTitle, GUILayout.Width(45f));
            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarTextField, GUILayout.MinWidth(160f));

            GUILayout.Space(10f);
            GUILayout.Label(currentTab == EditorTab.Dialogues ? "Dialogue workflow" : "Speaker workflow", Styles.ToolbarTitle);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RefreshAssetLists();
            }

            if (currentTab == EditorTab.Dialogues)
            {
                if (GUILayout.Button("New Dialogue", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    DialogueData asset = CreateDialogueAsset();
                    RefreshAssetLists();
                    SelectDialogue(asset);
                }

                using (new EditorGUI.DisabledGroupScope(selectedDialogue == null))
                {
                    if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        RunBuiltInValidation();
                    }

                    if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    {
                        EditorGUIUtility.PingObject(selectedDialogue);
                        Selection.activeObject = selectedDialogue;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("New Speaker", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    DialogueSpeakerData asset = CreateSpeakerAsset();
                    RefreshAssetLists();
                    SelectSpeaker(asset);
                }

                using (new EditorGUI.DisabledGroupScope(selectedSpeaker == null))
                {
                    if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    {
                        EditorGUIUtility.PingObject(selectedSpeaker);
                        Selection.activeObject = selectedSpeaker;
                    }
                }
            }
        }
    }

    private void DrawLeftPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPanelWidth)))
        {
            EditorGUILayout.Space(6f);

            if (currentTab == EditorTab.Dialogues)
            {
                EditorGUILayout.LabelField("Dialogue Assets", Styles.BigTitle);
                EditorGUILayout.LabelField("All DialogueData assets in the project.", Styles.WrappedMiniLabel);
                EditorGUILayout.Space(4f);

                leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

                foreach (DialogueData dialogue in GetFilteredDialogues())
                {
                    if (dialogue == null)
                        continue;

                    bool isSelected = dialogue == selectedDialogue;
                    DrawAssetButton(dialogue.name, GetDialogueSubLabel(dialogue), isSelected, delegate
                    {
                        SelectDialogue(dialogue);
                        Selection.activeObject = dialogue;
                    });
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("Speaker Assets", Styles.BigTitle);
                EditorGUILayout.LabelField("All DialogueSpeakerData assets in the project.", Styles.WrappedMiniLabel);
                EditorGUILayout.Space(4f);

                leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

                foreach (DialogueSpeakerData speaker in GetFilteredSpeakers())
                {
                    if (speaker == null)
                        continue;

                    bool isSelected = speaker == selectedSpeaker;
                    DrawAssetButton(speaker.name, GetSpeakerSubLabel(speaker), isSelected, delegate
                    {
                        SelectSpeaker(speaker);
                        Selection.activeObject = speaker;
                    });
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void DrawMiddlePanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(MiddlePanelWidth)))
        {
            EditorGUILayout.Space(6f);

            if (currentTab == EditorTab.Dialogues)
                DrawDialogueNodeColumn();
            else
                DrawSpeakerPreviewColumn();
        }
    }

    private void DrawRightPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.Space(6f);
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            if (currentTab == EditorTab.Dialogues)
                DrawDialogueInspectorColumn();
            else
                DrawSpeakerInspectorColumn();

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawDialogueNodeColumn()
    {
        if (selectedDialogue == null)
        {
            EditorGUILayout.HelpBox("Select a DialogueData asset on the left.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(selectedDialogue.name, Styles.BigTitle);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedDialogue), Styles.WrappedMiniLabel);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Line"))
                AddNode(DialogueNodeType.Line);

            if (GUILayout.Button("Add Choice"))
                AddNode(DialogueNodeType.Choice);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledGroupScope(!IsNodeSelectionValid()))
            {
                if (GUILayout.Button("Duplicate"))
                    DuplicateSelectedNode();

                if (GUILayout.Button("Delete"))
                    DeleteSelectedNode();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledGroupScope(selectedDialogue == null))
            {
                if (GUILayout.Button("Normalize Start"))
                    NormalizeStartNode();

                if (GUILayout.Button("Fill Empty Titles"))
                    FillMissingTitles();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Nodes", Styles.SectionTitle);
        EditorGUILayout.LabelField("Green = start node. Red = broken link. Yellow = warning.", Styles.WrappedMiniLabel);
        EditorGUILayout.Space(4f);

        middleScroll = EditorGUILayout.BeginScrollView(middleScroll);

        IReadOnlyList<DialogueNodeData> nodes = selectedDialogue.Nodes;
        if (nodes == null || nodes.Count == 0)
        {
            EditorGUILayout.HelpBox("This dialogue has no nodes yet.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                DrawNodeRow(i, nodes[i], nodes.Count);
                GUILayout.Space(4f);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawNodeRow(int nodeIndex, DialogueNodeData node, int totalNodeCount)
    {
        if (node == null)
            return;

        Rect rect = EditorGUILayout.GetControlRect(false, 58f);
        bool isSelected = nodeIndex == selectedNodeIndex;
        MessageKind kind = GetNodeHealth(node, totalNodeCount);

        Color background = isSelected ? new Color(0.20f, 0.55f, 0.95f, 0.18f) : new Color(0f, 0f, 0f, 0f);
        if (kind == MessageKind.Warning)
            background += new Color(0.85f, 0.65f, 0.05f, isSelected ? 0.12f : 0.08f);
        else if (kind == MessageKind.Error)
            background += new Color(0.95f, 0.25f, 0.20f, isSelected ? 0.16f : 0.10f);

        EditorGUI.DrawRect(rect, background);
        GUI.Box(rect, GUIContent.none);

        Rect buttonRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
        if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
        {
            selectedNodeIndex = nodeIndex;
            GUI.FocusControl(null);
        }

        Rect titleRect = new Rect(rect.x + 10f, rect.y + 7f, rect.width - 110f, 18f);
        EditorGUI.LabelField(titleRect, string.Format("{0:00}  {1}", nodeIndex, BuildNodeDisplayName(node)), EditorStyles.boldLabel);

        Rect infoRect = new Rect(rect.x + 10f, rect.y + 28f, rect.width - 120f, 16f);
        string info = BuildNodeInfoLine(node, totalNodeCount);
        EditorGUI.LabelField(infoRect, info, EditorStyles.miniLabel);

        Rect typeBadgeRect = new Rect(rect.xMax - 90f, rect.y + 8f, 38f, 18f);
        DrawBadge(typeBadgeRect, node.NodeType == DialogueNodeType.Line ? "LINE" : "CHOICE", node.NodeType == DialogueNodeType.Line ? new Color(0.25f, 0.65f, 1f, 0.55f) : new Color(0.85f, 0.45f, 1f, 0.55f));

        if (node.IsStartNode)
        {
            Rect startBadgeRect = new Rect(rect.xMax - 48f, rect.y + 8f, 38f, 18f);
            DrawBadge(startBadgeRect, "START", new Color(0.20f, 0.80f, 0.35f, 0.65f));
        }

        if (kind != MessageKind.Info)
        {
            Rect stateRect = new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, 12f);
            EditorGUI.LabelField(stateRect, kind == MessageKind.Error ? "Broken link or invalid data detected." : "Has minor issues or empty configuration.", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawSpeakerPreviewColumn()
    {
        if (selectedSpeaker == null)
        {
            EditorGUILayout.HelpBox("Select a DialogueSpeakerData asset on the left.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(selectedSpeaker.name, Styles.BigTitle);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedSpeaker), Styles.WrappedMiniLabel);
        EditorGUILayout.Space(10f);

        Texture previewTexture = null;
        if (selectedSpeaker.Portrait != null)
        {
            previewTexture = AssetPreview.GetAssetPreview(selectedSpeaker.Portrait);
            if (previewTexture == null)
                previewTexture = AssetPreview.GetMiniThumbnail(selectedSpeaker.Portrait);
        }

        Rect previewRect = GUILayoutUtility.GetRect(240f, 240f, GUILayout.ExpandWidth(true));
        GUI.Box(previewRect, GUIContent.none);

        if (previewTexture != null)
            GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
        else
            EditorGUI.LabelField(previewRect, "No portrait preview", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space(10f);
        EditorGUILayout.HelpBox("Use the right panel to edit localized name and portrait.", MessageType.Info);
    }

    private void DrawDialogueInspectorColumn()
    {
        if (selectedDialogue == null)
        {
            EditorGUILayout.HelpBox("Select a dialogue asset to edit.", MessageType.Info);
            return;
        }

        if (selectedDialogueSO == null)
            selectedDialogueSO = new SerializedObject(selectedDialogue);

        selectedDialogueSO.Update();

        DrawSectionHeader("Dialogue Settings");
        DrawDialogueRootProperties();

        EditorGUILayout.Space(10f);

        showValidationFoldout = EditorGUILayout.Foldout(showValidationFoldout, "Validation Summary", true);
        if (showValidationFoldout)
            DrawValidationSummary();

        EditorGUILayout.Space(10f);

        DrawSectionHeader("Selected Node");
        DrawSelectedNodeInspector();

        selectedDialogueSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedDialogue);
    }

    private void DrawDialogueRootProperties()
    {
        SerializedProperty dialogueIdProp = selectedDialogueSO.FindProperty("dialogueId");
        SerializedProperty repeatableProp = selectedDialogueSO.FindProperty("repeatable");
        SerializedProperty conditionsProp = selectedDialogueSO.FindProperty("conditions");

        if (dialogueIdProp != null)
            EditorGUILayout.PropertyField(dialogueIdProp, new GUIContent("Dialogue ID"));

        if (repeatableProp != null)
            EditorGUILayout.PropertyField(repeatableProp, new GUIContent("Repeatable"));

        if (conditionsProp != null)
            EditorGUILayout.PropertyField(conditionsProp, new GUIContent("Dialogue Conditions"), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run Built-In Validate"))
                RunBuiltInValidation();

            if (GUILayout.Button("Save Assets"))
                AssetDatabase.SaveAssets();
        }
    }

    private void DrawValidationSummary()
    {
        List<ValidationMessage> messages = BuildValidationMessages(selectedDialogue);

        if (messages.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found by the custom summary.", MessageType.Info);
            return;
        }

        int errorCount = messages.Count(delegate (ValidationMessage x) { return x.Kind == MessageKind.Error; });
        int warningCount = messages.Count(delegate (ValidationMessage x) { return x.Kind == MessageKind.Warning; });

        MessageType summaryType = errorCount > 0 ? MessageType.Error : MessageType.Warning;
        EditorGUILayout.HelpBox(string.Format("Errors: {0}   Warnings: {1}", errorCount, warningCount), summaryType);

        for (int i = 0; i < messages.Count; i++)
        {
            ValidationMessage message = messages[i];
            MessageType type = MessageType.Info;
            if (message.Kind == MessageKind.Warning)
                type = MessageType.Warning;
            else if (message.Kind == MessageKind.Error)
                type = MessageType.Error;

            EditorGUILayout.HelpBox(message.Text, type);
        }
    }

    private void DrawSelectedNodeInspector()
    {
        SerializedProperty nodesProp = selectedDialogueSO.FindProperty("nodes");
        if (nodesProp == null || nodesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("This dialogue has no nodes yet.", MessageType.Info);
            return;
        }

        if (selectedNodeIndex < 0 || selectedNodeIndex >= nodesProp.arraySize)
            selectedNodeIndex = 0;

        SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(selectedNodeIndex);
        if (nodeProp == null)
            return;

        EditorGUILayout.LabelField("Node Index", selectedNodeIndex.ToString());
        EditorGUILayout.Space(4f);

        SerializedProperty titleProp = nodeProp.FindPropertyRelative("inspectorNodeTitle");
        SerializedProperty nodeTypeProp = nodeProp.FindPropertyRelative("nodeType");
        SerializedProperty isStartNodeProp = nodeProp.FindPropertyRelative("isStartNode");
        SerializedProperty speakerModeProp = nodeProp.FindPropertyRelative("speakerMode");
        SerializedProperty customSpeakerNameProp = nodeProp.FindPropertyRelative("customSpeakerName");
        SerializedProperty speakerPortraitProp = nodeProp.FindPropertyRelative("speakerPortrait");
        SerializedProperty dialogueTextProp = nodeProp.FindPropertyRelative("dialogueText");
        SerializedProperty nextNodeIndexProp = nodeProp.FindPropertyRelative("nextNodeIndex");
        SerializedProperty choicesProp = nodeProp.FindPropertyRelative("choices");
        SerializedProperty conditionsProp = nodeProp.FindPropertyRelative("conditions");
        SerializedProperty onEnterActionsProp = nodeProp.FindPropertyRelative("onEnterActions");

        if (titleProp != null)
            EditorGUILayout.PropertyField(titleProp, new GUIContent("Inspector Title"));

        if (nodeTypeProp != null)
            EditorGUILayout.PropertyField(nodeTypeProp, new GUIContent("Node Type"));

        EditorGUILayout.BeginHorizontal();
        if (isStartNodeProp != null)
            EditorGUILayout.LabelField("Is Start Node", isStartNodeProp.boolValue ? "Yes" : "No");

        using (new EditorGUI.DisabledGroupScope(isStartNodeProp == null || isStartNodeProp.boolValue))
        {
            if (GUILayout.Button("Make Start", GUILayout.Width(95f)))
                SetStartNode(selectedNodeIndex);
        }
        EditorGUILayout.EndHorizontal();

        if (speakerModeProp != null)
            EditorGUILayout.PropertyField(speakerModeProp, new GUIContent("Speaker Mode"));

        if (speakerModeProp != null && speakerModeProp.enumValueIndex == (int)DialogueSpeakerMode.UseCustomName && customSpeakerNameProp != null)
            EditorGUILayout.PropertyField(customSpeakerNameProp, new GUIContent("Custom Speaker Name"), true);

        if (speakerPortraitProp != null)
            EditorGUILayout.PropertyField(speakerPortraitProp, new GUIContent("Speaker Portrait"));

        if (dialogueTextProp != null)
            EditorGUILayout.PropertyField(dialogueTextProp, new GUIContent("Dialogue Text"), true);

        EditorGUILayout.Space(8f);

        DialogueNodeType nodeType = (DialogueNodeType)nodeTypeProp.enumValueIndex;
        if (nodeType == DialogueNodeType.Line)
        {
            if (nextNodeIndexProp != null)
                EditorGUILayout.PropertyField(nextNodeIndexProp, new GUIContent("Next Node Index"));

            DrawNodeJumpButton(nextNodeIndexProp != null ? nextNodeIndexProp.intValue : -1, "Jump To Next Node");
        }
        else
        {
            if (choicesProp != null)
                EditorGUILayout.PropertyField(choicesProp, new GUIContent("Choices"), true);

            DrawChoiceJumpButtons();
        }

        EditorGUILayout.Space(8f);

        if (conditionsProp != null)
            EditorGUILayout.PropertyField(conditionsProp, new GUIContent("Node Conditions"), true);

        EditorGUILayout.Space(8f);

        if (onEnterActionsProp != null)
            EditorGUILayout.PropertyField(onEnterActionsProp, new GUIContent("On Enter Actions"), true);

        EditorGUILayout.Space(10f);
        DrawIncomingLinksBox();
    }

    private void DrawChoiceJumpButtons()
    {
        if (!IsNodeSelectionValid())
            return;

        DialogueNodeData node = selectedDialogue.Nodes[selectedNodeIndex];
        if (node == null || node.NodeType != DialogueNodeType.Choice || node.Choices == null || node.Choices.Count == 0)
            return;

        EditorGUILayout.LabelField("Choice Quick Jump", Styles.SectionTitle);

        for (int i = 0; i < node.Choices.Count; i++)
        {
            DialogueChoiceData choice = node.Choices[i];
            if (choice == null)
                continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                string label = string.Format("{0:00}  {1}  ->  {2}", i, string.IsNullOrWhiteSpace(choice.InspectorChoiceTitle) ? "Choice" : choice.InspectorChoiceTitle, choice.NextNodeIndex);
                EditorGUILayout.LabelField(label);

                using (new EditorGUI.DisabledGroupScope(choice.NextNodeIndex < 0 || choice.NextNodeIndex >= selectedDialogue.Nodes.Count))
                {
                    if (GUILayout.Button("Go", GUILayout.Width(44f)))
                    {
                        selectedNodeIndex = choice.NextNodeIndex;
                        GUI.FocusControl(null);
                    }
                }
            }
        }
    }

    private void DrawIncomingLinksBox()
    {
        if (!IsNodeSelectionValid())
            return;

        List<string> lines = BuildIncomingLinks(selectedNodeIndex);
        if (lines.Count == 0)
            return;

        EditorGUILayout.LabelField("Incoming Links", Styles.SectionTitle);
        foreach (string line in lines)
            EditorGUILayout.LabelField("• " + line, Styles.WrappedMiniLabel);
    }

    private void DrawSpeakerInspectorColumn()
    {
        if (selectedSpeaker == null)
        {
            EditorGUILayout.HelpBox("Select a speaker asset to edit.", MessageType.Info);
            return;
        }

        if (selectedSpeakerSO == null)
            selectedSpeakerSO = new SerializedObject(selectedSpeaker);

        selectedSpeakerSO.Update();

        DrawSectionHeader("Speaker Settings");

        SerializedProperty speakerNameProp = selectedSpeakerSO.FindProperty("speakerName");
        SerializedProperty portraitProp = selectedSpeakerSO.FindProperty("portrait");

        if (speakerNameProp != null)
            EditorGUILayout.PropertyField(speakerNameProp, new GUIContent("Speaker Name"), true);

        if (portraitProp != null)
            EditorGUILayout.PropertyField(portraitProp, new GUIContent("Portrait"));

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Assets"))
                AssetDatabase.SaveAssets();

            if (GUILayout.Button("Ping Asset"))
                EditorGUIUtility.PingObject(selectedSpeaker);
        }

        selectedSpeakerSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedSpeaker);
    }

    private void AddNode(DialogueNodeType nodeType)
    {
        if (selectedDialogue == null)
            return;

        Undo.RecordObject(selectedDialogue, "Add Dialogue Node");

        List<DialogueNodeData> nodes = GetOrCreateNodeList(selectedDialogue);
        DialogueNodeData node = new DialogueNodeData();

        SetFieldValue(node, "inspectorNodeTitle", string.Format(nodeType == DialogueNodeType.Line ? "Line {0:00}" : "Choice {0:00}", nodes.Count));
        SetFieldValue(node, "nodeType", nodeType);
        SetFieldValue(node, "isStartNode", nodes.Count == 0);
        SetFieldValue(node, "nextNodeIndex", -1);
        SetFieldValue(node, "choices", new List<DialogueChoiceData>());
        SetFieldValue(node, "conditions", new List<DialogueConditionData>());
        SetFieldValue(node, "onEnterActions", new List<DialogueActionData>());

        nodes.Add(node);

        SaveDialogueMutation();
        selectedNodeIndex = nodes.Count - 1;
    }

    private void DuplicateSelectedNode()
    {
        if (!IsNodeSelectionValid())
            return;

        Undo.RecordObject(selectedDialogue, "Duplicate Dialogue Node");

        List<DialogueNodeData> nodes = GetOrCreateNodeList(selectedDialogue);
        DialogueNodeData sourceNode = nodes[selectedNodeIndex];
        if (sourceNode == null)
            return;

        string json = JsonUtility.ToJson(sourceNode);
        DialogueNodeData copy = JsonUtility.FromJson<DialogueNodeData>(json);
        if (copy == null)
            return;

        string oldTitle = sourceNode.InspectorNodeTitle;
        if (string.IsNullOrWhiteSpace(oldTitle))
            oldTitle = sourceNode.NodeType == DialogueNodeType.Line ? "Line" : "Choice";

        SetFieldValue(copy, "inspectorNodeTitle", oldTitle + " Copy");
        SetFieldValue(copy, "isStartNode", false);

        nodes.Add(copy);

        SaveDialogueMutation();
        selectedNodeIndex = nodes.Count - 1;
    }

    private void DeleteSelectedNode()
    {
        if (!IsNodeSelectionValid())
            return;

        List<DialogueNodeData> nodes = GetOrCreateNodeList(selectedDialogue);
        DialogueNodeData node = nodes[selectedNodeIndex];

        string title = node != null ? node.InspectorNodeTitle : ("Node " + selectedNodeIndex);
        bool confirmed = EditorUtility.DisplayDialog(
            "Delete node",
            "Delete node [" + selectedNodeIndex + "] '" + title + "'?\n\nThe editor will also remap all next-node links.",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        Undo.RecordObject(selectedDialogue, "Delete Dialogue Node");

        bool deletedWasStart = node != null && node.IsStartNode;
        int deletedIndex = selectedNodeIndex;

        nodes.RemoveAt(deletedIndex);
        RemapNodeLinksAfterDelete(nodes, deletedIndex);

        if (deletedWasStart && nodes.Count > 0 && !HasAnyStartNode(nodes))
            SetFieldValue(nodes[0], "isStartNode", true);

        SaveDialogueMutation();
        selectedNodeIndex = Mathf.Clamp(selectedNodeIndex, 0, nodes.Count - 1);
        if (nodes.Count == 0)
            selectedNodeIndex = -1;
    }

    private void NormalizeStartNode()
    {
        if (selectedDialogue == null)
            return;

        Undo.RecordObject(selectedDialogue, "Normalize Start Node");

        List<DialogueNodeData> nodes = GetOrCreateNodeList(selectedDialogue);
        if (nodes.Count == 0)
            return;

        int firstStart = -1;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].IsStartNode)
            {
                firstStart = i;
                break;
            }
        }

        if (firstStart < 0)
            firstStart = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                continue;

            SetFieldValue(nodes[i], "isStartNode", i == firstStart);
        }

        SaveDialogueMutation();
        selectedNodeIndex = firstStart;
    }

    private void SetStartNode(int index)
    {
        if (selectedDialogue == null)
            return;

        List<DialogueNodeData> nodes = GetOrCreateNodeList(selectedDialogue);
        if (index < 0 || index >= nodes.Count)
            return;

        Undo.RecordObject(selectedDialogue, "Set Dialogue Start Node");

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                continue;

            SetFieldValue(nodes[i], "isStartNode", i == index);
        }

        SaveDialogueMutation();
        selectedNodeIndex = index;
    }

    private void FillMissingTitles()
    {
        if (selectedDialogue == null)
            return;

        Undo.RecordObject(selectedDialogue, "Fill Missing Dialogue Titles");

        List<DialogueNodeData> nodes = GetOrCreateNodeList(selectedDialogue);
        for (int i = 0; i < nodes.Count; i++)
        {
            DialogueNodeData node = nodes[i];
            if (node == null)
                continue;

            if (string.IsNullOrWhiteSpace(node.InspectorNodeTitle))
            {
                string nodeTitle = node.NodeType == DialogueNodeType.Line ? string.Format("Line {0:00}", i) : string.Format("Choice {0:00}", i);
                SetFieldValue(node, "inspectorNodeTitle", nodeTitle);
            }

            IReadOnlyList<DialogueChoiceData> nodeChoices = node.Choices;
            if (nodeChoices == null)
                continue;

            for (int c = 0; c < nodeChoices.Count; c++)
            {
                DialogueChoiceData choice = nodeChoices[c];
                if (choice == null)
                    continue;

                if (string.IsNullOrWhiteSpace(choice.InspectorChoiceTitle))
                    SetFieldValue(choice, "inspectorChoiceTitle", string.Format("Choice {0:00}.{1:00}", i, c));
            }
        }

        SaveDialogueMutation();
    }

    private void DrawNodeJumpButton(int targetIndex, string buttonLabel)
    {
        if (selectedDialogue == null)
            return;

        int count = selectedDialogue.Nodes != null ? selectedDialogue.Nodes.Count : 0;
        using (new EditorGUI.DisabledGroupScope(targetIndex < 0 || targetIndex >= count))
        {
            if (GUILayout.Button(buttonLabel))
            {
                selectedNodeIndex = targetIndex;
                GUI.FocusControl(null);
            }
        }
    }

    private void DrawAssetButton(string title, string subTitle, bool selected, Action onClick)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 50f);
        Color background = selected ? new Color(0.20f, 0.55f, 0.95f, 0.18f) : new Color(0f, 0f, 0f, 0.02f);
        EditorGUI.DrawRect(rect, background);
        GUI.Box(rect, GUIContent.none);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            if (onClick != null)
                onClick();
            GUI.FocusControl(null);
        }

        Rect titleRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f);
        Rect subRect = new Rect(rect.x + 8f, rect.y + 26f, rect.width - 16f, 18f);

        EditorGUI.LabelField(titleRect, title, EditorStyles.boldLabel);
        EditorGUI.LabelField(subRect, subTitle, EditorStyles.miniLabel);

        GUILayout.Space(4f);
    }

    private void DrawSectionHeader(string title)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 24f);
        EditorGUI.DrawRect(rect, new Color(0.17f, 0.17f, 0.17f, 1f));
        EditorGUI.LabelField(new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, rect.height - 6f), title, Styles.SectionTitle);
        EditorGUILayout.Space(2f);
    }

    private void DrawBadge(Rect rect, string text, Color color)
    {
        EditorGUI.DrawRect(rect, color);
        GUI.Label(rect, text, Styles.MiniBadge);
    }

    private MessageKind GetNodeHealth(DialogueNodeData node, int totalNodeCount)
    {
        if (node == null)
            return MessageKind.Error;

        bool hasWarning = false;

        if (string.IsNullOrWhiteSpace(node.InspectorNodeTitle))
            hasWarning = true;

        if (node.NodeType == DialogueNodeType.Line)
        {
            if (node.NextNodeIndex < -1 || node.NextNodeIndex >= totalNodeCount)
                return MessageKind.Error;
        }
        else
        {
            if (node.Choices == null || node.Choices.Count == 0)
                return MessageKind.Error;

            for (int i = 0; i < node.Choices.Count; i++)
            {
                DialogueChoiceData choice = node.Choices[i];
                if (choice == null)
                    return MessageKind.Error;

                if (choice.NextNodeIndex < -1 || choice.NextNodeIndex >= totalNodeCount)
                    return MessageKind.Error;

                if (string.IsNullOrWhiteSpace(choice.InspectorChoiceTitle))
                    hasWarning = true;
            }
        }

        return hasWarning ? MessageKind.Warning : MessageKind.Info;
    }

    private string BuildNodeDisplayName(DialogueNodeData node)
    {
        if (node == null)
            return "(Null node)";

        string title = string.IsNullOrWhiteSpace(node.InspectorNodeTitle)
            ? (node.NodeType == DialogueNodeType.Line ? "Line" : "Choice")
            : node.InspectorNodeTitle;

        return title;
    }

    private string BuildNodeInfoLine(DialogueNodeData node, int totalNodeCount)
    {
        if (node == null)
            return "Missing node";

        if (node.NodeType == DialogueNodeType.Line)
        {
            if (node.NextNodeIndex < 0)
                return "Ends dialogue";

            bool valid = node.NextNodeIndex < totalNodeCount;
            return valid ? "Next -> " + node.NextNodeIndex : "Next -> INVALID";
        }

        int choiceCount = node.Choices != null ? node.Choices.Count : 0;
        return "Choices: " + choiceCount;
    }

    private string GetDialogueSubLabel(DialogueData dialogue)
    {
        if (dialogue == null)
            return string.Empty;

        int nodeCount = dialogue.Nodes != null ? dialogue.Nodes.Count : 0;
        int startIndex = dialogue.GetStartNodeIndex();
        string startText = startIndex >= 0 ? ("start " + startIndex) : "no start";
        return string.Format("{0} nodes, {1}", nodeCount, startText);
    }

    private string GetSpeakerSubLabel(DialogueSpeakerData speaker)
    {
        if (speaker == null)
            return string.Empty;

        return speaker.Portrait != null ? "Has portrait" : "No portrait";
    }

    private List<ValidationMessage> BuildValidationMessages(DialogueData dialogue)
    {
        List<ValidationMessage> messages = new List<ValidationMessage>();
        if (dialogue == null)
            return messages;

        IReadOnlyList<DialogueNodeData> nodes = dialogue.Nodes;
        int nodeCount = nodes != null ? nodes.Count : 0;

        if (!dialogue.Repeatable && string.IsNullOrWhiteSpace(dialogue.DialogueId))
            messages.Add(new ValidationMessage(MessageKind.Warning, "Dialogue is non-repeatable, but Dialogue ID is empty. Progress tracking may break."));

        if (nodeCount == 0)
        {
            messages.Add(new ValidationMessage(MessageKind.Error, "Dialogue has no nodes."));
            return messages;
        }

        int startCount = 0;
        for (int i = 0; i < nodeCount; i++)
        {
            DialogueNodeData node = nodes[i];
            if (node != null && node.IsStartNode)
                startCount++;
        }

        if (startCount == 0)
            messages.Add(new ValidationMessage(MessageKind.Error, "Dialogue has no start node."));
        else if (startCount > 1)
            messages.Add(new ValidationMessage(MessageKind.Warning, "Dialogue has multiple start nodes. Keep only one."));

        for (int i = 0; i < nodeCount; i++)
        {
            DialogueNodeData node = nodes[i];
            if (node == null)
            {
                messages.Add(new ValidationMessage(MessageKind.Error, "Node " + i + " is null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.InspectorNodeTitle))
                messages.Add(new ValidationMessage(MessageKind.Warning, "Node " + i + " has empty inspector title."));

            if (node.NodeType == DialogueNodeType.Line)
            {
                if (node.NextNodeIndex < -1 || node.NextNodeIndex >= nodeCount)
                    messages.Add(new ValidationMessage(MessageKind.Error, "Line node " + i + " points to invalid next index " + node.NextNodeIndex + "."));
            }
            else
            {
                if (node.Choices == null || node.Choices.Count == 0)
                {
                    messages.Add(new ValidationMessage(MessageKind.Error, "Choice node " + i + " has no choices."));
                    continue;
                }

                for (int c = 0; c < node.Choices.Count; c++)
                {
                    DialogueChoiceData choice = node.Choices[c];
                    if (choice == null)
                    {
                        messages.Add(new ValidationMessage(MessageKind.Error, "Choice node " + i + " contains null choice at index " + c + "."));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(choice.InspectorChoiceTitle))
                        messages.Add(new ValidationMessage(MessageKind.Warning, "Choice node " + i + ", choice " + c + " has empty inspector title."));

                    if (choice.NextNodeIndex < -1 || choice.NextNodeIndex >= nodeCount)
                        messages.Add(new ValidationMessage(MessageKind.Error, "Choice node " + i + ", choice " + c + " points to invalid next index " + choice.NextNodeIndex + "."));
                }
            }
        }

        return messages;
    }

    private List<string> BuildIncomingLinks(int targetNodeIndex)
    {
        List<string> lines = new List<string>();
        if (selectedDialogue == null || selectedDialogue.Nodes == null)
            return lines;

        for (int i = 0; i < selectedDialogue.Nodes.Count; i++)
        {
            DialogueNodeData node = selectedDialogue.Nodes[i];
            if (node == null)
                continue;

            if (node.NodeType == DialogueNodeType.Line)
            {
                if (node.NextNodeIndex == targetNodeIndex)
                    lines.Add(string.Format("Node {0:00} -> Next", i));
            }
            else if (node.Choices != null)
            {
                for (int c = 0; c < node.Choices.Count; c++)
                {
                    DialogueChoiceData choice = node.Choices[c];
                    if (choice != null && choice.NextNodeIndex == targetNodeIndex)
                        lines.Add(string.Format("Node {0:00} -> Choice {1:00}", i, c));
                }
            }
        }

        return lines;
    }

    private void RunBuiltInValidation()
    {
        if (selectedDialogue == null)
            return;

        selectedDialogue.ValidateDialogue();
        EditorUtility.SetDirty(selectedDialogue);
        AssetDatabase.SaveAssets();
    }

    private void RemapNodeLinksAfterDelete(List<DialogueNodeData> nodes, int deletedIndex)
    {
        if (nodes == null)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            DialogueNodeData node = nodes[i];
            if (node == null)
                continue;

            if (node.NodeType == DialogueNodeType.Line)
            {
                int next = node.NextNodeIndex;
                if (next == deletedIndex)
                    SetFieldValue(node, "nextNodeIndex", -1);
                else if (next > deletedIndex)
                    SetFieldValue(node, "nextNodeIndex", next - 1);
            }

            IReadOnlyList<DialogueChoiceData> choices = node.Choices;
            if (choices == null)
                continue;

            for (int c = 0; c < choices.Count; c++)
            {
                DialogueChoiceData choice = choices[c];
                if (choice == null)
                    continue;

                int next = choice.NextNodeIndex;
                if (next == deletedIndex)
                    SetFieldValue(choice, "nextNodeIndex", -1);
                else if (next > deletedIndex)
                    SetFieldValue(choice, "nextNodeIndex", next - 1);
            }
        }
    }

    private bool IsNodeSelectionValid()
    {
        return selectedDialogue != null &&
               selectedDialogue.Nodes != null &&
               selectedNodeIndex >= 0 &&
               selectedNodeIndex < selectedDialogue.Nodes.Count;
    }

    private void RefreshAssetLists()
    {
        dialogueAssets = AssetDatabase.FindAssets("t:DialogueData", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<DialogueData>)
            .Where(delegate (DialogueData x) { return x != null; })
            .OrderBy(delegate (DialogueData x) { return x.name; })
            .ToList();

        speakerAssets = AssetDatabase.FindAssets("t:DialogueSpeakerData", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<DialogueSpeakerData>)
            .Where(delegate (DialogueSpeakerData x) { return x != null; })
            .OrderBy(delegate (DialogueSpeakerData x) { return x.name; })
            .ToList();

        if (selectedDialogue != null && !dialogueAssets.Contains(selectedDialogue))
        {
            selectedDialogue = null;
            selectedDialogueSO = null;
            selectedNodeIndex = -1;
        }

        if (selectedSpeaker != null && !speakerAssets.Contains(selectedSpeaker))
        {
            selectedSpeaker = null;
            selectedSpeakerSO = null;
        }
    }

    private IEnumerable<DialogueData> GetFilteredDialogues()
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return dialogueAssets;

        return dialogueAssets.Where(delegate (DialogueData x)
        {
            return x != null && x.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    private IEnumerable<DialogueSpeakerData> GetFilteredSpeakers()
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return speakerAssets;

        return speakerAssets.Where(delegate (DialogueSpeakerData x)
        {
            return x != null && x.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        });
    }

    private void TryAdoptCurrentSelection()
    {
        DialogueData dialogue = Selection.activeObject as DialogueData;
        if (dialogue != null)
        {
            currentTab = EditorTab.Dialogues;
            SelectDialogue(dialogue);
            return;
        }

        DialogueSpeakerData speaker = Selection.activeObject as DialogueSpeakerData;
        if (speaker != null)
        {
            currentTab = EditorTab.Speakers;
            SelectSpeaker(speaker);
        }
    }

    private void SelectDialogue(DialogueData dialogue)
    {
        selectedDialogue = dialogue;
        selectedDialogueSO = dialogue != null ? new SerializedObject(dialogue) : null;

        if (selectedDialogue == null || selectedDialogue.Nodes == null || selectedDialogue.Nodes.Count == 0)
            selectedNodeIndex = -1;
        else
            selectedNodeIndex = Mathf.Clamp(selectedNodeIndex, 0, selectedDialogue.Nodes.Count - 1);
    }

    private void SelectSpeaker(DialogueSpeakerData speaker)
    {
        selectedSpeaker = speaker;
        selectedSpeakerSO = speaker != null ? new SerializedObject(speaker) : null;
    }

    private void RebuildSerializedObjects()
    {
        if (selectedDialogue != null)
            selectedDialogueSO = new SerializedObject(selectedDialogue);

        if (selectedSpeaker != null)
            selectedSpeakerSO = new SerializedObject(selectedSpeaker);
    }

    private void ApplyPendingSerializedChanges()
    {
        if (selectedDialogueSO != null && selectedDialogueSO.hasModifiedProperties)
        {
            selectedDialogueSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedDialogue);
        }

        if (selectedSpeakerSO != null && selectedSpeakerSO.hasModifiedProperties)
        {
            selectedSpeakerSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectedSpeaker);
        }
    }

    private void SaveDialogueMutation()
    {
        EditorUtility.SetDirty(selectedDialogue);
        AssetDatabase.SaveAssets();
        RebuildSerializedObjects();
        Repaint();
    }

    private static DialogueData CreateDialogueAsset()
    {
        EnsureFolderExists(DialogueFolder);

        DialogueData asset = ScriptableObject.CreateInstance<DialogueData>();
        string path = AssetDatabase.GenerateUniqueAssetPath(DialogueFolder + "/Dialogue_New.asset");

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        return asset;
    }

    private static DialogueSpeakerData CreateSpeakerAsset()
    {
        EnsureFolderExists(SpeakerFolder);

        DialogueSpeakerData asset = ScriptableObject.CreateInstance<DialogueSpeakerData>();
        string path = AssetDatabase.GenerateUniqueAssetPath(SpeakerFolder + "/DialogueSpeaker_New.asset");

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        return asset;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        if (parts.Length == 0)
            return;

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static List<DialogueNodeData> GetOrCreateNodeList(DialogueData dialogue)
    {
        List<DialogueNodeData> nodes = GetFieldValue<List<DialogueNodeData>>(dialogue, "nodes");
        if (nodes == null)
        {
            nodes = new List<DialogueNodeData>();
            SetFieldValue(dialogue, "nodes", nodes);
        }

        return nodes;
    }

    private static T GetFieldValue<T>(object owner, string fieldName)
    {
        if (owner == null)
            return default(T);

        FieldInfo field = owner.GetType().GetField(fieldName, FieldFlags);
        if (field == null)
            return default(T);

        object value = field.GetValue(owner);
        if (value == null)
            return default(T);

        return (T)value;
    }

    private static void SetFieldValue(object owner, string fieldName, object value)
    {
        if (owner == null)
            return;

        FieldInfo field = owner.GetType().GetField(fieldName, FieldFlags);
        if (field == null)
            return;

        field.SetValue(owner, value);
    }

    private static bool HasAnyStartNode(List<DialogueNodeData> nodes)
    {
        if (nodes == null)
            return false;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].IsStartNode)
                return true;
        }

        return false;
    }
}
#endif
