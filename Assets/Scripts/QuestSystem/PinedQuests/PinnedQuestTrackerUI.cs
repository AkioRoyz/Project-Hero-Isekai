using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class PinnedQuestTrackerUI : MonoBehaviour
{
    [Header("Visual Roots")]
    [Tooltip("Корневой визуальный объект HUD. Его можно скрывать, но объект со скриптом должен оставаться активным.")]
    [SerializeField] private GameObject visualRoot;

    [Tooltip("Контейнер со строками закреплённых квестов.")]
    [SerializeField] private GameObject contentRoot;

    [Tooltip("Необязательный объект для состояния, когда закреплённых квестов нет.")]
    [SerializeField] private GameObject emptyRoot;

    [Header("Rows")]
    [SerializeField] private PinnedQuestRowUI[] pinnedRows = new PinnedQuestRowUI[5];

    [Header("Quest Type Colors")]
    [SerializeField] private Color mainQuestColor = Color.yellow;
    [SerializeField] private Color sideQuestColor = Color.white;

    [Header("Completed Objective Display")]
    [Tooltip("Alpha в hex-формате для выполненных задач. Например 66 = полупрозрачно.")]
    [SerializeField] private string completedObjectiveAlphaHex = "66";

    [Header("Task Formatting")]
    [SerializeField] private string bulletPrefix = "• ";
    [SerializeField] private string emptyTaskFallback = "";

    [Header("Ready To Turn In Text")]
    [SerializeField] private LocalizedString readyToTurnInText;

    private bool isSubscribed;

    private void Awake()
    {
        EnsureLocalizationReady();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
        TrySubscribeQuestManager();
        RefreshUI();
    }

    private void Start()
    {
        TrySubscribeQuestManager();
        RefreshUI();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
        UnsubscribeQuestManager();
    }

    private void HandleSelectedLocaleChanged(Locale locale)
    {
        RefreshUI();
    }

    [ContextMenu("Force Refresh")]
    public void RefreshUI()
    {
        EnsureLocalizationReady();
        TrySubscribeQuestManager();

        HideAllRows();

        if (QuestManager.Instance == null)
        {
            SetVisualRootVisible(false);
            SetContentVisible(false);
            SetEmptyVisible(false);
            return;
        }

        List<QuestRuntimeData> pinnedQuests = QuestManager.Instance.GetPinnedActiveQuests();

        if (pinnedQuests == null || pinnedQuests.Count == 0)
        {
            SetVisualRootVisible(false);
            SetContentVisible(false);
            SetEmptyVisible(false);
            return;
        }

        pinnedQuests.Sort(ComparePinnedQuests);

        SetVisualRootVisible(true);
        SetContentVisible(true);
        SetEmptyVisible(false);

        int rowCount = Mathf.Min(pinnedRows.Length, pinnedQuests.Count);

        for (int i = 0; i < rowCount; i++)
        {
            PinnedQuestRowUI row = pinnedRows[i];
            QuestRuntimeData runtime = pinnedQuests[i];

            if (row == null || runtime == null)
                continue;

            QuestData questData = QuestManager.Instance.GetQuestData(runtime.QuestId);
            if (questData == null)
            {
                row.Hide();
                continue;
            }

            string title = GetLocalizedString(questData.QuestTitle, questData.QuestId);
            string tasks = BuildCurrentTasksText(questData, runtime);
            Color color = questData.QuestType == QuestType.Main ? mainQuestColor : sideQuestColor;

            row.Show();
            row.SetData(title, tasks, color);
        }

        for (int i = rowCount; i < pinnedRows.Length; i++)
        {
            if (pinnedRows[i] != null)
            {
                pinnedRows[i].Hide();
            }
        }
    }

    private int ComparePinnedQuests(QuestRuntimeData a, QuestRuntimeData b)
    {
        if (QuestManager.Instance == null)
            return 0;

        QuestData questA = QuestManager.Instance.GetQuestData(a.QuestId);
        QuestData questB = QuestManager.Instance.GetQuestData(b.QuestId);

        if (questA == null && questB == null) return 0;
        if (questA == null) return 1;
        if (questB == null) return -1;

        int typeCompare = questA.QuestType.CompareTo(questB.QuestType);
        if (typeCompare != 0)
            return typeCompare;

        string titleA = GetLocalizedString(questA.QuestTitle, questA.QuestId);
        string titleB = GetLocalizedString(questB.QuestTitle, questB.QuestId);

        return string.Compare(titleA, titleB, System.StringComparison.CurrentCulture);
    }

    private string BuildCurrentTasksText(QuestData questData, QuestRuntimeData runtime)
    {
        if (questData == null || runtime == null)
            return emptyTaskFallback;

        if (runtime.QuestState == QuestState.ReadyToTurnIn)
        {
            return GetReadyToTurnInText();
        }

        if (runtime.QuestState != QuestState.Active)
        {
            return emptyTaskFallback;
        }

        QuestStepRuntimeData currentStepRuntime = runtime.GetCurrentStep();
        QuestStepData currentStepData = GetCurrentStepData(questData, runtime.CurrentStepIndex);

        if (currentStepRuntime == null)
            return emptyTaskFallback;

        List<string> lines = new();

        if (currentStepRuntime.Objectives != null && currentStepRuntime.Objectives.Count > 0)
        {
            for (int i = 0; i < currentStepRuntime.Objectives.Count; i++)
            {
                QuestObjectiveRuntimeData runtimeObjective = currentStepRuntime.Objectives[i];
                if (runtimeObjective == null)
                    continue;

                QuestObjectiveData staticObjective = GetStaticObjectiveData(currentStepData, i);

                string baseText = staticObjective != null
                    ? GetLocalizedString(staticObjective.ObjectiveDescription, runtimeObjective.ObjectiveId)
                    : runtimeObjective.ObjectiveId;

                string lineText;

                if (runtimeObjective.RequiredAmount > 1)
                {
                    lineText = $"{bulletPrefix}{baseText} ({runtimeObjective.CurrentAmount}/{runtimeObjective.RequiredAmount})";
                }
                else
                {
                    lineText = $"{bulletPrefix}{baseText}";
                }

                if (runtimeObjective.IsCompleted)
                {
                    lineText = WrapWithAlphaTag(lineText, completedObjectiveAlphaHex);
                }

                lines.Add(lineText);
            }
        }

        if (lines.Count > 0)
        {
            return string.Join("\n", lines);
        }

        if (currentStepData != null)
        {
            string fallbackTask = GetLocalizedString(currentStepData.StepTaskText, string.Empty);

            if (string.IsNullOrWhiteSpace(fallbackTask))
            {
                fallbackTask = GetLocalizedString(currentStepData.StepDescription, currentStepRuntime.StepId);
            }

            return fallbackTask;
        }

        return emptyTaskFallback;
    }

    private string GetReadyToTurnInText()
    {
        return GetLocalizedString(readyToTurnInText, "• Квест можно сдать");
    }

    private string WrapWithAlphaTag(string text, string alphaHex)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(alphaHex))
            return text;

        return $"<alpha=#{alphaHex}>{text}</alpha>";
    }

    private QuestStepData GetCurrentStepData(QuestData questData, int stepIndex)
    {
        if (questData == null || questData.Steps == null)
            return null;

        if (stepIndex < 0 || stepIndex >= questData.Steps.Length)
            return null;

        return questData.Steps[stepIndex];
    }

    private QuestObjectiveData GetStaticObjectiveData(QuestStepData stepData, int objectiveIndex)
    {
        if (stepData == null || stepData.Objectives == null)
            return null;

        if (objectiveIndex < 0 || objectiveIndex >= stepData.Objectives.Length)
            return null;

        return stepData.Objectives[objectiveIndex];
    }

    private void EnsureLocalizationReady()
    {
        var initOperation = LocalizationSettings.InitializationOperation;
        if (!initOperation.IsDone)
        {
            initOperation.WaitForCompletion();
        }
    }

    private string GetLocalizedString(LocalizedString localizedString, string fallback)
    {
        if (localizedString == null || localizedString.IsEmpty)
            return fallback ?? string.Empty;

        EnsureLocalizationReady();

        var handle = localizedString.GetLocalizedStringAsync();
        if (!handle.IsDone)
        {
            handle.WaitForCompletion();
        }

        string result = handle.Result;
        return !string.IsNullOrEmpty(result) ? result : (fallback ?? string.Empty);
    }

    private void HideAllRows()
    {
        for (int i = 0; i < pinnedRows.Length; i++)
        {
            if (pinnedRows[i] != null)
            {
                pinnedRows[i].Hide();
            }
        }
    }

    private void SetVisualRootVisible(bool visible)
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(visible);
        }
    }

    private void SetContentVisible(bool visible)
    {
        if (contentRoot != null)
        {
            contentRoot.SetActive(visible);
        }
    }

    private void SetEmptyVisible(bool visible)
    {
        if (emptyRoot != null)
        {
            emptyRoot.SetActive(visible);
        }
    }

    private void TrySubscribeQuestManager()
    {
        if (isSubscribed)
            return;

        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestListChanged += HandleQuestDataChanged;
        isSubscribed = true;
    }

    private void UnsubscribeQuestManager()
    {
        if (!isSubscribed)
            return;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListChanged -= HandleQuestDataChanged;
        }

        isSubscribed = false;
    }

    private void HandleQuestDataChanged()
    {
        RefreshUI();
    }
}