using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class QuestJournalUI : MonoBehaviour
{
    private enum JournalSection
    {
        Active,
        Completed
    }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Lists")]
    [SerializeField] private QuestJournalListUI activeQuestListUI;
    [SerializeField] private QuestJournalListUI completedQuestListUI;

    [Header("Tab Visuals")]
    [SerializeField] private GameObject mainTabSelectedObject;
    [SerializeField] private GameObject sideTabSelectedObject;

    [Header("Section Labels")]
    [SerializeField] private TextMeshProUGUI activeSectionLabel;
    [SerializeField] private TextMeshProUGUI completedSectionLabel;
    [SerializeField] private LocalizedString activeSectionText;
    [SerializeField] private LocalizedString completedSectionText;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailRoot;
    [SerializeField] private GameObject noSelectionRoot;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questFullDescriptionText;
    [SerializeField] private TextMeshProUGUI questStateText;
    [SerializeField] private TextMeshProUGUI currentStepTitleText;
    [SerializeField] private QuestObjectiveRowUI[] objectiveRows = new QuestObjectiveRowUI[8];

    [Header("Pin Button")]
    [SerializeField] private GameObject pinButtonObject;
    [SerializeField] private Button pinButton;
    [SerializeField] private TextMeshProUGUI pinButtonText;
    [SerializeField] private LocalizedString pinText;
    [SerializeField] private LocalizedString unpinText;

    [Header("Pin Limit Message")]
    [SerializeField] private GameObject pinLimitMessageObject;
    [SerializeField] private TextMeshProUGUI pinLimitMessageText;
    [SerializeField] private LocalizedString pinLimitMessage;

    [Header("Quest State Labels")]
    [SerializeField] private LocalizedString stateNotStartedText;
    [SerializeField] private LocalizedString stateActiveText;
    [SerializeField] private LocalizedString stateReadyToTurnInText;
    [SerializeField] private LocalizedString stateCompletedText;
    [SerializeField] private LocalizedString stateFailedText;

    private QuestType currentTab = QuestType.Main;
    private JournalSection currentSection = JournalSection.Active;

    private int activeSelectedIndex = 0;
    private int completedSelectedIndex = 0;

    private List<QuestJournalListUI.EntryViewData> cachedActiveEntries = new();
    private List<QuestJournalListUI.EntryViewData> cachedCompletedEntries = new();

    public bool IsOpen => root != null && root.activeSelf;

    private void OnEnable()
    {
        SubscribeQuestManagerEvents();
        LocalizationSettings.InitializationOperation.Completed += HandleLocalizationInitialized;
        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
        RefreshUI();
    }

    private void OnDisable()
    {
        LocalizationSettings.InitializationOperation.Completed -= HandleLocalizationInitialized;
        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
        UnsubscribeQuestManagerEvents();
    }

    private void HandleLocalizationInitialized(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.Localization.Settings.LocalizationSettings> _)
    {
        RefreshUI();
    }

    private void HandleSelectedLocaleChanged(UnityEngine.Localization.Locale _)
    {
        RefreshUI();
    }

    private void Start()
    {
        HidePinLimitMessage();
        RefreshUI();
    }

    public void Open()
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        HidePinLimitMessage();
        RefreshUI();
    }

    public void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        HidePinLimitMessage();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void SelectMainTab()
    {
        currentTab = QuestType.Main;
        ValidateCurrentSection();
        RefreshUI();
    }

    public void SelectSideTab()
    {
        currentTab = QuestType.Side;
        ValidateCurrentSection();
        RefreshUI();
    }

    public void FocusActiveSection()
    {
        currentSection = JournalSection.Active;
        ValidateCurrentSection();
        RefreshUI();
    }

    public void FocusCompletedSection()
    {
        currentSection = JournalSection.Completed;
        ValidateCurrentSection();
        RefreshUI();
    }

    public void MoveSelectionUp()
    {
        HidePinLimitMessage();

        if (currentSection == JournalSection.Active)
        {
            if (activeQuestListUI != null)
            {
                activeQuestListUI.MoveSelection(-1);
                activeSelectedIndex = activeQuestListUI.GetSelectedIndex();
            }
        }
        else
        {
            if (completedQuestListUI != null)
            {
                completedQuestListUI.MoveSelection(-1);
                completedSelectedIndex = completedQuestListUI.GetSelectedIndex();
            }
        }

        RefreshDetailsOnly();
    }

    public void MoveSelectionDown()
    {
        HidePinLimitMessage();

        if (currentSection == JournalSection.Active)
        {
            if (activeQuestListUI != null)
            {
                activeQuestListUI.MoveSelection(1);
                activeSelectedIndex = activeQuestListUI.GetSelectedIndex();
            }
        }
        else
        {
            if (completedQuestListUI != null)
            {
                completedQuestListUI.MoveSelection(1);
                completedSelectedIndex = completedQuestListUI.GetSelectedIndex();
            }
        }

        RefreshDetailsOnly();
    }

    public void HandleJournalSelectInput()
    {
        HidePinLimitMessage();

        if (currentSection == JournalSection.Active)
        {
            if (cachedCompletedEntries != null && cachedCompletedEntries.Count > 0)
            {
                FocusCompletedSection();
            }
        }
    }

    public void HandleJournalBackInput()
    {
        HidePinLimitMessage();

        if (currentSection == JournalSection.Completed)
        {
            if (cachedActiveEntries != null && cachedActiveEntries.Count > 0)
            {
                FocusActiveSection();
            }
        }
    }

    public void TogglePinForSelectedQuest()
    {
        HidePinLimitMessage();

        QuestJournalListUI.EntryViewData selectedEntry = GetSelectedEntry();
        if (selectedEntry == null)
            return;

        if (QuestManager.Instance == null)
            return;

        QuestRuntimeData runtime = QuestManager.Instance.GetQuestRuntime(selectedEntry.QuestId);
        if (runtime == null)
            return;

        if (runtime.QuestState != QuestState.Active)
            return;

        bool isPinned = QuestManager.Instance.IsQuestPinned(selectedEntry.QuestId);

        if (isPinned)
        {
            QuestManager.Instance.UnpinQuest(selectedEntry.QuestId);
        }
        else
        {
            bool pinned = QuestManager.Instance.PinQuest(selectedEntry.QuestId);

            if (!pinned)
            {
                ShowPinLimitMessage();
                RefreshDetailsOnly();
                return;
            }
        }

        RefreshDetailsOnly();
    }

    private void SubscribeQuestManagerEvents()
    {
        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestListChanged -= HandleQuestDataChanged;
        QuestManager.Instance.OnQuestListChanged += HandleQuestDataChanged;
    }

    private void UnsubscribeQuestManagerEvents()
    {
        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestListChanged -= HandleQuestDataChanged;
    }

    private void HandleQuestDataChanged()
    {
        if (!IsOpen)
            return;

        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshTabVisuals();
        RefreshSectionLabels();
        RebuildQuestLists();
        RefreshListVisuals();
        RefreshDetailsOnly();
    }

    private void RefreshTabVisuals()
    {
        if (mainTabSelectedObject != null)
        {
            mainTabSelectedObject.SetActive(currentTab == QuestType.Main);
        }

        if (sideTabSelectedObject != null)
        {
            sideTabSelectedObject.SetActive(currentTab == QuestType.Side);
        }
    }

    private void RefreshSectionLabels()
    {
        if (activeSectionLabel != null)
        {
            activeSectionLabel.text = GetLocalizedString(activeSectionText, "Активные");
        }

        if (completedSectionLabel != null)
        {
            completedSectionLabel.text = GetLocalizedString(completedSectionText, "Завершённые");
        }
    }

    private void RebuildQuestLists()
    {
        cachedActiveEntries = BuildEntriesForSection(currentTab, true);
        cachedCompletedEntries = BuildEntriesForSection(currentTab, false);

        if (activeQuestListUI != null)
        {
            activeQuestListUI.SetData(cachedActiveEntries);
            activeQuestListUI.SetVisualState(currentSection == JournalSection.Active, activeSelectedIndex);
            activeSelectedIndex = activeQuestListUI.GetSelectedIndex();
        }

        if (completedQuestListUI != null)
        {
            completedQuestListUI.SetData(cachedCompletedEntries);
            completedQuestListUI.SetVisualState(currentSection == JournalSection.Completed, completedSelectedIndex);
            completedSelectedIndex = completedQuestListUI.GetSelectedIndex();
        }

        ValidateCurrentSection();
    }

    private void RefreshListVisuals()
    {
        if (activeQuestListUI != null)
        {
            activeQuestListUI.SetVisualState(currentSection == JournalSection.Active, activeSelectedIndex);
            activeSelectedIndex = activeQuestListUI.GetSelectedIndex();
        }

        if (completedQuestListUI != null)
        {
            completedQuestListUI.SetVisualState(currentSection == JournalSection.Completed, completedSelectedIndex);
            completedSelectedIndex = completedQuestListUI.GetSelectedIndex();
        }
    }

    private void RefreshDetailsOnly()
    {
        RefreshListVisuals();

        QuestJournalListUI.EntryViewData selectedEntry = GetSelectedEntry();
        if (selectedEntry == null || QuestManager.Instance == null)
        {
            ShowNoSelection();
            return;
        }

        QuestData questData = QuestManager.Instance.GetQuestData(selectedEntry.QuestId);
        QuestRuntimeData runtime = QuestManager.Instance.GetQuestRuntime(selectedEntry.QuestId);

        if (questData == null || runtime == null)
        {
            ShowNoSelection();
            return;
        }

        ShowDetail();

        if (questTitleText != null)
        {
            questTitleText.text = GetQuestTitle(questData);
        }

        if (questFullDescriptionText != null)
        {
            questFullDescriptionText.text = GetLocalizedString(questData.FullDescription, selectedEntry.QuestId);
        }

        if (questStateText != null)
        {
            questStateText.text = GetQuestStateLabel(runtime.QuestState);
        }

        RefreshCurrentStepBlock(questData, runtime);
        RefreshPinButton(selectedEntry.QuestId, runtime);
    }

    private void RefreshCurrentStepBlock(QuestData questData, QuestRuntimeData runtime)
    {
        ClearObjectiveRows();

        if (runtime.QuestState != QuestState.Active)
        {
            if (currentStepTitleText != null)
            {
                if (runtime.QuestState == QuestState.Completed)
                {
                    currentStepTitleText.text = GetLocalizedString(stateCompletedText, "Завершён");
                }
                else if (runtime.QuestState == QuestState.Failed)
                {
                    currentStepTitleText.text = GetLocalizedString(stateFailedText, "Провален");
                }
                else if (runtime.QuestState == QuestState.ReadyToTurnIn)
                {
                    currentStepTitleText.text = GetLocalizedString(stateReadyToTurnInText, "Можно сдать");
                }
                else
                {
                    currentStepTitleText.text = string.Empty;
                }
            }

            return;
        }

        QuestStepRuntimeData currentStepRuntime = runtime.GetCurrentStep();
        if (currentStepRuntime == null)
        {
            if (currentStepTitleText != null)
            {
                currentStepTitleText.text = string.Empty;
            }

            return;
        }

        QuestStepData currentStepData = GetCurrentStepData(questData, runtime.CurrentStepIndex);

        if (currentStepTitleText != null)
        {
            currentStepTitleText.text = currentStepData != null
                ? GetLocalizedString(currentStepData.StepDescription, currentStepRuntime.StepId)
                : currentStepRuntime.StepId;
        }

        if (currentStepRuntime.Objectives == null || currentStepRuntime.Objectives.Count == 0)
            return;

        for (int i = 0; i < objectiveRows.Length; i++)
        {
            if (objectiveRows[i] == null)
                continue;

            if (i >= currentStepRuntime.Objectives.Count)
            {
                objectiveRows[i].gameObject.SetActive(false);
                continue;
            }

            QuestObjectiveRuntimeData runtimeObjective = currentStepRuntime.Objectives[i];
            QuestObjectiveData staticObjective = GetStaticObjectiveData(currentStepData, i);

            string text = GetObjectiveDisplayText(runtimeObjective, staticObjective);

            objectiveRows[i].gameObject.SetActive(true);
            objectiveRows[i].SetData(text, runtimeObjective != null && runtimeObjective.IsCompleted);
        }
    }

    private void RefreshPinButton(string questId, QuestRuntimeData runtime)
    {
        bool canShow = runtime != null && runtime.QuestState == QuestState.Active;

        if (pinButtonObject != null)
        {
            pinButtonObject.SetActive(canShow);
        }

        if (!canShow)
            return;

        bool isPinned = QuestManager.Instance != null && QuestManager.Instance.IsQuestPinned(questId);

        if (pinButtonText != null)
        {
            pinButtonText.text = isPinned
                ? GetLocalizedString(unpinText, "Открепить")
                : GetLocalizedString(pinText, "Закрепить");
        }

        if (pinButton != null)
        {
            pinButton.interactable = true;
        }
    }

    private List<QuestJournalListUI.EntryViewData> BuildEntriesForSection(QuestType questType, bool activeSection)
    {
        List<QuestJournalListUI.EntryViewData> result = new();

        if (QuestManager.Instance == null)
            return result;

        IReadOnlyCollection<QuestRuntimeData> source = activeSection
            ? QuestManager.Instance.ActiveQuests
            : QuestManager.Instance.CompletedQuests;

        foreach (QuestRuntimeData runtime in source)
        {
            if (runtime == null)
                continue;

            QuestData questData = QuestManager.Instance.GetQuestData(runtime.QuestId);
            if (questData == null)
                continue;

            if (questData.QuestType != questType)
                continue;

            result.Add(new QuestJournalListUI.EntryViewData(
                runtime.QuestId,
                GetQuestTitle(questData),
                GetLocalizedString(questData.ShortDescription, runtime.QuestId)
            ));
        }

        result.Sort((a, b) => string.Compare(a.Title, b.Title, System.StringComparison.CurrentCulture));
        return result;
    }

    private void ValidateCurrentSection()
    {
        bool hasActive = cachedActiveEntries != null && cachedActiveEntries.Count > 0;
        bool hasCompleted = cachedCompletedEntries != null && cachedCompletedEntries.Count > 0;

        if (currentSection == JournalSection.Active && !hasActive && hasCompleted)
        {
            currentSection = JournalSection.Completed;
        }
        else if (currentSection == JournalSection.Completed && !hasCompleted && hasActive)
        {
            currentSection = JournalSection.Active;
        }

        if (!hasActive && !hasCompleted)
        {
            currentSection = JournalSection.Active;
        }
    }

    private QuestJournalListUI.EntryViewData GetSelectedEntry()
    {
        if (currentSection == JournalSection.Active)
        {
            return activeQuestListUI != null ? activeQuestListUI.GetSelectedEntry() : null;
        }

        return completedQuestListUI != null ? completedQuestListUI.GetSelectedEntry() : null;
    }

    private void ShowDetail()
    {
        if (detailRoot != null)
        {
            detailRoot.SetActive(true);
        }

        if (noSelectionRoot != null)
        {
            noSelectionRoot.SetActive(false);
        }
    }

    private void ShowNoSelection()
    {
        if (detailRoot != null)
        {
            detailRoot.SetActive(false);
        }

        if (noSelectionRoot != null)
        {
            noSelectionRoot.SetActive(true);
        }

        HidePinLimitMessage();
    }

    private void ClearObjectiveRows()
    {
        for (int i = 0; i < objectiveRows.Length; i++)
        {
            if (objectiveRows[i] != null)
            {
                objectiveRows[i].gameObject.SetActive(false);
            }
        }
    }

    private void ShowPinLimitMessage()
    {
        if (pinLimitMessageObject != null)
        {
            pinLimitMessageObject.SetActive(true);
        }

        if (pinLimitMessageText != null)
        {
            pinLimitMessageText.text = GetLocalizedString(pinLimitMessage, "Можно закрепить не более 5 квестов.");
        }
    }

    private void HidePinLimitMessage()
    {
        if (pinLimitMessageObject != null)
        {
            pinLimitMessageObject.SetActive(false);
        }
    }

    private string GetQuestTitle(QuestData questData)
    {
        if (questData == null)
            return "NULL";

        return GetLocalizedString(questData.QuestTitle, questData.QuestId);
    }

    private string GetObjectiveDisplayText(QuestObjectiveRuntimeData runtimeObjective, QuestObjectiveData staticObjective)
    {
        if (runtimeObjective == null)
            return string.Empty;

        string baseText = staticObjective != null
            ? GetLocalizedString(staticObjective.ObjectiveDescription, runtimeObjective.ObjectiveId)
            : runtimeObjective.ObjectiveId;

        if (runtimeObjective.RequiredAmount > 1)
        {
            return $"{baseText} ({runtimeObjective.CurrentAmount}/{runtimeObjective.RequiredAmount})";
        }

        return baseText;
    }

    private QuestStepData GetCurrentStepData(QuestData questData, int currentStepIndex)
    {
        if (questData == null || questData.Steps == null)
            return null;

        if (currentStepIndex < 0 || currentStepIndex >= questData.Steps.Length)
            return null;

        return questData.Steps[currentStepIndex];
    }

    private QuestObjectiveData GetStaticObjectiveData(QuestStepData stepData, int objectiveIndex)
    {
        if (stepData == null || stepData.Objectives == null)
            return null;

        if (objectiveIndex < 0 || objectiveIndex >= stepData.Objectives.Length)
            return null;

        return stepData.Objectives[objectiveIndex];
    }

    private string GetQuestStateLabel(QuestState state)
    {
        switch (state)
        {
            case QuestState.Active:
                return GetLocalizedString(stateActiveText, "Активен");

            case QuestState.ReadyToTurnIn:
                return GetLocalizedString(stateReadyToTurnInText, "Можно сдать");

            case QuestState.Completed:
                return GetLocalizedString(stateCompletedText, "Завершён");

            case QuestState.Failed:
                return GetLocalizedString(stateFailedText, "Провален");

            case QuestState.NotStarted:
            default:
                return GetLocalizedString(stateNotStartedText, "Не начат");
        }
    }

    private string GetLocalizedString(LocalizedString localizedString, string fallback)
    {
        if (localizedString == null || localizedString.IsEmpty)
            return fallback ?? string.Empty;

        var handle = localizedString.GetLocalizedStringAsync();

        if (!handle.IsDone)
            return fallback ?? string.Empty;

        string result = handle.Result;
        return !string.IsNullOrEmpty(result) ? result : (fallback ?? string.Empty);
    }
}