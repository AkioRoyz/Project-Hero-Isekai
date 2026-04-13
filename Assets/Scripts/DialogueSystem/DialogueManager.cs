using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public class DialogueManager : MonoBehaviour
{
    private class RuntimeChoiceEntry
    {
        public DialogueChoiceData SourceChoice;
        public bool IsSelectable;
        public string DisplayText;

        public RuntimeChoiceEntry(DialogueChoiceData sourceChoice, bool isSelectable, string displayText)
        {
            SourceChoice = sourceChoice;
            IsSelectable = isSelectable;
            DisplayText = displayText;
        }
    }

    public static DialogueManager Instance;

    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private ExpSystem expSystem;

    [Header("Optional Runtime Providers")]
    [SerializeField] private MonoBehaviour questProviderBehaviour;
    [SerializeField] private MonoBehaviour questActionHandlerBehaviour;

    private IDialogueQuestProvider questProvider;
    private IDialogueActionQuestHandler questActionHandler;

    private int uiRefreshVersion = 0;

    private DialogueData currentDialogue;
    private DialogueContext currentContext;
    private int currentNodeIndex = -1;
    private int selectedChoiceIndex = 0;
    private bool isDialogueActive = false;

    private readonly List<RuntimeChoiceEntry> visibleChoices = new();
    private readonly HashSet<int> enteredNodeIndices = new();

    public bool IsDialogueActive => isDialogueActive;
    public DialogueData CurrentDialogue => currentDialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        questProvider = questProviderBehaviour as IDialogueQuestProvider;
        questActionHandler = questActionHandlerBehaviour as IDialogueActionQuestHandler;
    }

    private void Start()
    {
        if (dialogueUI != null)
        {
            dialogueUI.Hide();
        }
    }

    private void OnEnable()
    {
        if (gameInput != null)
        {
            gameInput.OnDialogueUp += HandleDialogueUp;
            gameInput.OnDialogueDown += HandleDialogueDown;
            gameInput.OnDialogueSelect += HandleDialogueSelect;
        }
    }

    private void OnDisable()
    {
        if (gameInput != null)
        {
            gameInput.OnDialogueUp -= HandleDialogueUp;
            gameInput.OnDialogueDown -= HandleDialogueDown;
            gameInput.OnDialogueSelect -= HandleDialogueSelect;
        }
    }

    public bool CanStartDialogue(DialogueData dialogueData, IDialogueSource source = null)
    {
        if (dialogueData == null)
            return false;

        if (isDialogueActive)
            return false;

        if (!IsDialogueRepeatableOrNotCompleted(dialogueData))
            return false;

        DialogueContext context = BuildContext(source);

        if (!AreConditionsMet(dialogueData.Conditions, context))
            return false;

        int startNodeIndex = dialogueData.GetStartNodeIndex();
        if (startNodeIndex < 0)
            return false;

        int validNodeIndex = FindFirstValidNodeFrom(startNodeIndex, dialogueData, context);
        return validNodeIndex >= 0;
    }

    public bool StartDialogue(DialogueData dialogueData, IDialogueSource source = null)
    {
        if (dialogueData == null)
        {
            Debug.LogWarning("StartDialogue called with null dialogueData.");
            return false;
        }

        if (isDialogueActive)
        {
            Debug.LogWarning("Cannot start dialogue: another dialogue is already active.");
            return false;
        }

        if (!IsDialogueRepeatableOrNotCompleted(dialogueData))
        {
            Debug.Log($"Dialogue {dialogueData.name} is non-repeatable and has already been completed.");
            return false;
        }

        DialogueContext context = BuildContext(source);

        if (!AreConditionsMet(dialogueData.Conditions, context))
        {
            Debug.Log($"Dialogue {dialogueData.name} cannot start because dialogue conditions are not met.");
            return false;
        }

        int startNodeIndex = dialogueData.GetStartNodeIndex();
        if (startNodeIndex < 0)
        {
            Debug.LogWarning($"Dialogue {dialogueData.name} has no start node.");
            return false;
        }

        int firstValidNodeIndex = FindFirstValidNodeFrom(startNodeIndex, dialogueData, context);
        if (firstValidNodeIndex < 0)
        {
            Debug.LogWarning($"Dialogue {dialogueData.name} has no valid node to start from.");
            return false;
        }

        currentDialogue = dialogueData;
        currentContext = context;
        currentNodeIndex = firstValidNodeIndex;
        selectedChoiceIndex = 0;
        isDialogueActive = true;
        enteredNodeIndices.Clear();
        visibleChoices.Clear();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Dialogue);
        }

        if (gameInput != null)
        {
            gameInput.SwitchToDialogueMode();
        }

        if (dialogueUI != null)
        {
            dialogueUI.Show();
        }

        ShowCurrentNode();
        return true;
    }

    public void CloseDialogue()
    {
        if (!isDialogueActive)
            return;

        MarkDialogueCompletedIfNeeded();

        currentDialogue = null;
        currentContext = null;
        currentNodeIndex = -1;
        selectedChoiceIndex = 0;
        isDialogueActive = false;
        visibleChoices.Clear();
        enteredNodeIndices.Clear();

        if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
            dialogueUI.SetSpeakerName(string.Empty);
            dialogueUI.SetDialogueText(string.Empty);
            dialogueUI.SetPortrait(null);
            dialogueUI.Hide();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }

        if (gameInput != null)
        {
            gameInput.SwitchToPlayerMode();
        }
    }

    private DialogueContext BuildContext(IDialogueSource source)
    {
        int playerLevel = 1;

        if (expSystem != null)
        {
            playerLevel = expSystem.CurrentLvl;
        }

        return new DialogueContext(source, playerLevel, questProvider, questActionHandler);
    }

    private bool IsDialogueRepeatableOrNotCompleted(DialogueData dialogueData)
    {
        if (dialogueData == null)
            return false;

        if (dialogueData.Repeatable)
            return true;

        if (DialogueRuntimeState.Instance == null)
            return true;

        return !DialogueRuntimeState.Instance.IsDialogueCompleted(dialogueData.DialogueId);
    }

    private void MarkDialogueCompletedIfNeeded()
    {
        if (currentDialogue == null)
            return;

        if (currentDialogue.Repeatable)
            return;

        if (DialogueRuntimeState.Instance == null)
            return;

        DialogueRuntimeState.Instance.MarkDialogueCompleted(currentDialogue.DialogueId);
    }

    private void ShowCurrentNode()
    {
        if (!isDialogueActive || currentDialogue == null)
        {
            CloseDialogue();
            return;
        }

        DialogueNodeData node = GetCurrentValidNode();
        if (node == null)
        {
            Debug.LogWarning("No valid current dialogue node was found. Closing dialogue.");
            CloseDialogue();
            return;
        }

        ExecuteNodeEnterActionsIfNeeded(node);

        BuildVisibleChoices(node);
        ResetSelectedChoiceIndexToFirstSelectable();
        RefreshCurrentNodeUI(node);
    }

    private DialogueNodeData GetCurrentValidNode()
    {
        if (currentDialogue == null)
            return null;

        DialogueNodeData currentNode = currentDialogue.GetNode(currentNodeIndex);
        if (currentNode != null && AreConditionsMet(currentNode.Conditions, currentContext))
        {
            return currentNode;
        }

        int nextValidIndex = FindFirstValidNodeFrom(currentNodeIndex + 1, currentDialogue, currentContext);
        if (nextValidIndex < 0)
            return null;

        currentNodeIndex = nextValidIndex;
        return currentDialogue.GetNode(currentNodeIndex);
    }

    private void ExecuteNodeEnterActionsIfNeeded(DialogueNodeData node)
    {
        if (node == null)
            return;

        if (enteredNodeIndices.Contains(currentNodeIndex))
            return;

        enteredNodeIndices.Add(currentNodeIndex);
        ExecuteActions(node.OnEnterActions);
    }

    private void BuildVisibleChoices(DialogueNodeData node)
    {
        visibleChoices.Clear();

        if (node == null || node.NodeType != DialogueNodeType.Choice)
            return;

        for (int i = 0; i < node.Choices.Count; i++)
        {
            DialogueChoiceData choice = node.Choices[i];
            bool isAvailable = AreConditionsMet(choice.Conditions, currentContext);

            if (isAvailable)
            {
                visibleChoices.Add(new RuntimeChoiceEntry(choice, true, string.Empty));
            }
            else if (choice.UnavailableMode == DialogueChoiceUnavailableMode.ShowDisabled)
            {
                visibleChoices.Add(new RuntimeChoiceEntry(choice, false, string.Empty));
            }
        }
    }

    private void ResetSelectedChoiceIndexToFirstSelectable()
    {
        selectedChoiceIndex = 0;

        for (int i = 0; i < visibleChoices.Count; i++)
        {
            if (visibleChoices[i].IsSelectable)
            {
                selectedChoiceIndex = i;
                return;
            }
        }

        selectedChoiceIndex = -1;
    }

    private async void RefreshCurrentNodeUI(DialogueNodeData node)
    {
        if (dialogueUI == null || node == null)
            return;

        int refreshVersion = ++uiRefreshVersion;

        dialogueUI.ShowLoadingState();

        string speakerName = await ResolveSpeakerName(node);
        string dialogueText = await ResolveLocalizedString(node.DialogueText);
        Sprite portrait = ResolvePortrait(node);

        if (!isDialogueActive || refreshVersion != uiRefreshVersion)
            return;

        List<DialogueUI.ChoiceViewData> choiceViewData = null;

        if (node.NodeType == DialogueNodeType.Choice)
        {
            choiceViewData = new List<DialogueUI.ChoiceViewData>();

            for (int i = 0; i < visibleChoices.Count; i++)
            {
                RuntimeChoiceEntry entry = visibleChoices[i];
                string text;

                if (entry.IsSelectable)
                {
                    text = await ResolveLocalizedString(entry.SourceChoice.ChoiceText);
                }
                else
                {
                    if (entry.SourceChoice.DisabledChoiceText != null)
                    {
                        string disabledText = await ResolveLocalizedString(entry.SourceChoice.DisabledChoiceText);
                        text = string.IsNullOrEmpty(disabledText)
                            ? await ResolveLocalizedString(entry.SourceChoice.ChoiceText)
                            : disabledText;
                    }
                    else
                    {
                        text = await ResolveLocalizedString(entry.SourceChoice.ChoiceText);
                    }
                }

                if (!isDialogueActive || refreshVersion != uiRefreshVersion)
                    return;

                entry.DisplayText = text;
                choiceViewData.Add(new DialogueUI.ChoiceViewData(
                    text,
                    entry.IsSelectable,
                    entry.SourceChoice.IsQuestRelated,
                    entry.SourceChoice.ShowQuestMarker
                ));
            }

            if (choiceViewData.Count == 0)
            {
                CloseDialogue();
                return;
            }
        }

        if (!isDialogueActive || refreshVersion != uiRefreshVersion)
            return;

        dialogueUI.SetSpeakerName(speakerName);
        dialogueUI.SetDialogueText(dialogueText);
        dialogueUI.SetPortrait(portrait);

        if (node.NodeType == DialogueNodeType.Choice)
        {
            dialogueUI.SetChoices(choiceViewData, selectedChoiceIndex);
            dialogueUI.HideContinueIndicator();
        }
        else
        {
            dialogueUI.ClearChoices();
            dialogueUI.ShowContinueIndicator();
        }

        dialogueUI.HideLoadingState();
    }

    private async System.Threading.Tasks.Task<string> ResolveSpeakerName(DialogueNodeData node)
    {
        if (node == null)
            return string.Empty;

        LocalizedString localizedName = null;

        if (node.SpeakerMode == DialogueSpeakerMode.UseCustomName)
        {
            localizedName = node.CustomSpeakerName;
        }
        else
        {
            localizedName = currentContext?.GetSourceSpeakerName();
        }

        return await ResolveLocalizedString(localizedName);
    }

    private Sprite ResolvePortrait(DialogueNodeData node)
    {
        if (node == null)
            return null;

        if (node.SpeakerPortrait != null)
            return node.SpeakerPortrait;

        return currentContext?.GetSourcePortrait();
    }

    private async System.Threading.Tasks.Task<string> ResolveLocalizedString(LocalizedString localizedString)
    {
        if (localizedString == null)
            return string.Empty;

        try
        {
            return await localizedString.GetLocalizedStringAsync().Task;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void HandleDialogueUp()
    {
        if (!isDialogueActive)
            return;

        if (visibleChoices.Count == 0)
            return;

        MoveSelection(-1);
    }

    private void HandleDialogueDown()
    {
        if (!isDialogueActive)
            return;

        if (visibleChoices.Count == 0)
            return;

        MoveSelection(1);
    }

    private void MoveSelection(int direction)
    {
        if (visibleChoices.Count == 0)
            return;

        if (!HasAnySelectableChoice())
            return;

        int newIndex = selectedChoiceIndex;

        for (int i = 0; i < visibleChoices.Count; i++)
        {
            newIndex += direction;

            if (newIndex < 0)
            {
                newIndex = visibleChoices.Count - 1;
            }
            else if (newIndex >= visibleChoices.Count)
            {
                newIndex = 0;
            }

            if (visibleChoices[newIndex].IsSelectable)
            {
                selectedChoiceIndex = newIndex;
                RefreshChoiceSelection();
                return;
            }
        }
    }

    private bool HasAnySelectableChoice()
    {
        for (int i = 0; i < visibleChoices.Count; i++)
        {
            if (visibleChoices[i].IsSelectable)
                return true;
        }

        return false;
    }

    private void RefreshChoiceSelection()
    {
        if (dialogueUI == null)
            return;

        List<DialogueUI.ChoiceViewData> choiceViewData = new();

        for (int i = 0; i < visibleChoices.Count; i++)
        {
            RuntimeChoiceEntry entry = visibleChoices[i];
            choiceViewData.Add(new DialogueUI.ChoiceViewData(
                entry.DisplayText,
                entry.IsSelectable,
                entry.SourceChoice.IsQuestRelated,
                entry.SourceChoice.ShowQuestMarker
            ));
        }

        dialogueUI.SetChoices(choiceViewData, selectedChoiceIndex);
    }

    private void HandleDialogueSelect()
    {
        if (!isDialogueActive || currentDialogue == null)
            return;

        DialogueNodeData node = GetCurrentValidNode();
        if (node == null)
        {
            CloseDialogue();
            return;
        }

        if (node.NodeType == DialogueNodeType.Line)
        {
            GoToNode(node.NextNodeIndex);
        }
        else if (node.NodeType == DialogueNodeType.Choice)
        {
            if (visibleChoices.Count == 0)
            {
                CloseDialogue();
                return;
            }

            if (selectedChoiceIndex < 0 || selectedChoiceIndex >= visibleChoices.Count)
                return;

            RuntimeChoiceEntry selectedEntry = visibleChoices[selectedChoiceIndex];

            if (!selectedEntry.IsSelectable)
                return;

            ExecuteActions(selectedEntry.SourceChoice.OnSelectActions);
            GoToNode(selectedEntry.SourceChoice.NextNodeIndex);
        }
    }

    private void GoToNode(int nodeIndex)
    {
        if (nodeIndex < 0)
        {
            CloseDialogue();
            return;
        }

        if (currentDialogue == null)
        {
            CloseDialogue();
            return;
        }

        int validNodeIndex = FindFirstValidNodeFrom(nodeIndex, currentDialogue, currentContext);
        if (validNodeIndex < 0)
        {
            Debug.LogWarning($"Could not find a valid node starting from index {nodeIndex}. Dialogue will be closed.");
            CloseDialogue();
            return;
        }

        currentNodeIndex = validNodeIndex;
        selectedChoiceIndex = 0;
        ShowCurrentNode();
    }

    private int FindFirstValidNodeFrom(int startIndex, DialogueData dialogueData, DialogueContext context)
    {
        if (dialogueData == null)
            return -1;

        if (startIndex < 0)
            return -1;

        for (int i = startIndex; i < dialogueData.Nodes.Count; i++)
        {
            DialogueNodeData node = dialogueData.GetNode(i);

            if (node == null)
                continue;

            if (AreConditionsMet(node.Conditions, context))
            {
                return i;
            }
        }

        return -1;
    }

    private bool AreConditionsMet(IReadOnlyList<DialogueConditionData> conditions, DialogueContext context)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            if (!IsConditionMet(conditions[i], context))
                return false;
        }

        return true;
    }

    private bool IsConditionMet(DialogueConditionData condition, DialogueContext context)
    {
        if (condition == null)
            return true;

        switch (condition.ConditionType)
        {
            case DialogueConditionType.None:
                return true;

            case DialogueConditionType.PlayerLevelAtLeast:
                return context != null && context.GetPlayerLevel() >= condition.RequiredLevel;

            case DialogueConditionType.HasItem:
                return InventorySystem.Instance != null &&
                       condition.RequiredItem != null &&
                       InventorySystem.Instance.HasItem(condition.RequiredItem, condition.RequiredItemAmount);

            case DialogueConditionType.DoesNotHaveItem:
                return InventorySystem.Instance != null &&
                       condition.RequiredItem != null &&
                       !InventorySystem.Instance.HasItem(condition.RequiredItem, condition.RequiredItemAmount);

            case DialogueConditionType.PlayOnce:
                return DialogueRuntimeState.Instance == null ||
                       !DialogueRuntimeState.Instance.HasPlayed(condition.OnceKey);

            case DialogueConditionType.QuestState:
                if (context == null || context.QuestProvider == null)
                    return false;

                return context.QuestProvider.IsQuestStateMatched(condition.QuestId, condition.RequiredQuestState);

            case DialogueConditionType.QuestStepId:
                if (context == null || context.QuestProvider == null)
                    return false;

                return context.QuestProvider.IsQuestStepIdMatched(condition.QuestId, condition.RequiredQuestStepId);

            default:
                return true;
        }
    }

    private void ExecuteActions(IReadOnlyList<DialogueActionData> actions)
    {
        if (actions == null || actions.Count == 0)
            return;

        for (int i = 0; i < actions.Count; i++)
        {
            ExecuteAction(actions[i]);
        }
    }

    private void ExecuteAction(DialogueActionData action)
    {
        if (action == null)
            return;

        switch (action.ActionType)
        {
            case DialogueActionType.None:
                break;

            case DialogueActionType.GiveReward:
                if (RewardSystem.Instance != null)
                {
                    RewardSystem.Instance.GiveReward(action.RewardData);
                }
                break;

            case DialogueActionType.RemoveItem:
                if (InventorySystem.Instance != null &&
                    action.ItemToRemove != null &&
                    action.ItemRemoveAmount > 0)
                {
                    bool removed = InventorySystem.Instance.RemoveItem(action.ItemToRemove, action.ItemRemoveAmount);

                    if (!removed)
                    {
                        Debug.LogWarning($"Failed to remove item {action.ItemToRemove.name} x{action.ItemRemoveAmount}");
                    }
                }
                break;

            case DialogueActionType.MarkPlayed:
                if (DialogueRuntimeState.Instance != null)
                {
                    DialogueRuntimeState.Instance.MarkPlayed(action.PlayedKey);
                }
                break;

            case DialogueActionType.QuestAction:
                {
                    if (questActionHandler == null)
                    {
                        Debug.LogWarning("DialogueManager: questActionHandler is missing.");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(action.QuestId))
                    {
                        Debug.LogWarning("DialogueManager: QuestAction has empty questId.");
                        break;
                    }

                    bool handled = questActionHandler.HandleQuestAction(action.QuestId, action.QuestActionType);

                    if (!handled)
                    {
                        Debug.LogWarning($"DialogueManager: failed to handle quest action '{action.QuestActionType}' for quest '{action.QuestId}'.");
                    }

                    break;
                }

            case DialogueActionType.AcceptQuestObjective:
                {
                    if (questActionHandler == null)
                    {
                        Debug.LogWarning("DialogueManager: questActionHandler is missing.");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(action.QuestId))
                    {
                        Debug.LogWarning("DialogueManager: AcceptQuestObjective has empty questId.");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(action.QuestObjectiveId))
                    {
                        Debug.LogWarning("DialogueManager: AcceptQuestObjective has empty questObjectiveId.");
                        break;
                    }

                    int amount = Mathf.Max(1, action.QuestObjectiveAmount);
                    bool handled = questActionHandler.AcceptQuestObjective(action.QuestId, action.QuestObjectiveId, amount);

                    if (!handled)
                    {
                        Debug.LogWarning($"DialogueManager: failed to accept objective '{action.QuestObjectiveId}' for quest '{action.QuestId}'.");
                    }

                    break;
                }
        }
    }
}