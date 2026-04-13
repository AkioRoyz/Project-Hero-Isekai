using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour, IDialogueQuestProvider, IDialogueActionQuestHandler
{
    public static QuestManager Instance;

    public const int MaxPinnedQuests = 5;

    [Header("Quest Database")]
    [SerializeField] private QuestData[] allQuestData = Array.Empty<QuestData>();

    [Header("Runtime Sources")]
    [SerializeField] private ExpSystem expSystem;

    [Header("Behaviour")]
    [SerializeField] private bool autoPinAcceptedQuests = true;

    private readonly Dictionary<string, QuestData> questDataById = new();
    private readonly Dictionary<string, QuestRuntimeData> activeQuests = new();
    private readonly Dictionary<string, QuestRuntimeData> completedQuests = new();
    private readonly Dictionary<string, QuestRuntimeData> failedQuests = new();

    private bool inventorySubscribed;

    public event Action<QuestData> OnQuestAccepted;
    public event Action<QuestData> OnQuestReadyToTurnIn;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<QuestData> OnQuestFailed;
    public event Action<QuestData> OnQuestAdvanced;
    public event Action<QuestData> OnQuestProgressChanged;
    public event Action OnQuestListChanged;
    public event Action OnPinnedQuestsChanged;

    public IReadOnlyCollection<QuestRuntimeData> ActiveQuests => activeQuests.Values;
    public IReadOnlyCollection<QuestRuntimeData> CompletedQuests => completedQuests.Values;
    public IReadOnlyCollection<QuestRuntimeData> FailedQuests => failedQuests.Values;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildQuestDatabase();
    }

    private void OnEnable()
    {
        if (expSystem != null)
        {
            expSystem.OnLevelChange += HandleLevelChanged;
        }

        TrySubscribeInventorySystem();
        OnQuestCompleted += HandleQuestCompletedForProgressAndAutoStart;
    }

    private void Start()
    {
        TrySubscribeInventorySystem();
        TryAutoStartQuestsByCurrentLevel();
        TryAutoStartQuestsByInventory();
        TryAutoStartQuestsByCompletedQuestState();
        RefreshItemObjectivesForAllActiveQuests();
        OnQuestListChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (expSystem != null)
        {
            expSystem.OnLevelChange -= HandleLevelChanged;
        }

        if (inventorySubscribed && InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= HandleInventoryChanged;
            inventorySubscribed = false;
        }

        OnQuestCompleted -= HandleQuestCompletedForProgressAndAutoStart;
    }

    private void TrySubscribeInventorySystem()
    {
        if (inventorySubscribed)
            return;

        if (InventorySystem.Instance == null)
            return;

        InventorySystem.Instance.OnInventoryChanged += HandleInventoryChanged;
        inventorySubscribed = true;
    }

    private void BuildQuestDatabase()
    {
        questDataById.Clear();

        if (allQuestData == null)
            return;

        for (int i = 0; i < allQuestData.Length; i++)
        {
            QuestData questData = allQuestData[i];

            if (questData == null)
            {
                Debug.LogWarning("QuestManager: one of allQuestData entries is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(questData.QuestId))
            {
                Debug.LogWarning($"QuestManager: questData '{questData.name}' has empty QuestId.");
                continue;
            }

            if (questDataById.ContainsKey(questData.QuestId))
            {
                Debug.LogWarning($"QuestManager: duplicate QuestId found: {questData.QuestId}");
                continue;
            }

            questDataById.Add(questData.QuestId, questData);
        }
    }

    public QuestData GetQuestData(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        questDataById.TryGetValue(questId, out QuestData questData);
        return questData;
    }

    public QuestRuntimeData GetQuestRuntime(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        if (activeQuests.TryGetValue(questId, out QuestRuntimeData activeRuntime))
            return activeRuntime;

        if (completedQuests.TryGetValue(questId, out QuestRuntimeData completedRuntime))
            return completedRuntime;

        if (failedQuests.TryGetValue(questId, out QuestRuntimeData failedRuntime))
            return failedRuntime;

        return null;
    }

    public bool HasQuest(string questId)
    {
        QuestRuntimeData runtime = GetQuestRuntime(questId);
        return runtime != null && runtime.QuestState != QuestState.NotStarted;
    }

    public QuestState GetQuestState(string questId)
    {
        QuestRuntimeData runtime = GetQuestRuntime(questId);

        if (runtime == null)
            return QuestState.NotStarted;

        return runtime.QuestState;
    }

    public bool IsQuestStateMatched(string questId, QuestState requiredQuestState)
    {
        QuestState currentState = GetQuestState(questId);
        return currentState == requiredQuestState;
    }

    public bool IsQuestStepIdMatched(string questId, string requiredQuestStepId)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(requiredQuestStepId))
            return false;

        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        if (runtimeData.QuestState != QuestState.Active)
            return false;

        QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
        if (currentStep == null)
            return false;

        return string.Equals(currentStep.StepId, requiredQuestStepId, StringComparison.Ordinal);
    }

    public bool CanAcceptQuest(string questId)
    {
        QuestData questData = GetQuestData(questId);
        if (questData == null)
            return false;

        if (activeQuests.ContainsKey(questId))
            return false;

        if (completedQuests.ContainsKey(questId))
        {
            return questData.RepeatMode == QuestRepeatMode.Repeatable;
        }

        if (failedQuests.ContainsKey(questId))
        {
            return questData.CanRestartAfterFail;
        }

        return true;
    }

    public bool AreQuestAvailabilityConditionsMet(string questId)
    {
        QuestData questData = GetQuestData(questId);
        if (questData == null)
            return false;

        return AreQuestAvailabilityConditionsMet(questData);
    }

    public bool CanQuestBeOfferedNow(string questId)
    {
        QuestData questData = GetQuestData(questId);
        if (questData == null)
            return false;

        if (!CanAcceptQuest(questId))
            return false;

        return AreQuestAvailabilityConditionsMet(questData);
    }

    public bool AcceptQuest(string questId)
    {
        if (!CanQuestBeOfferedNow(questId))
            return false;

        QuestData questData = GetQuestData(questId);
        if (questData == null)
            return false;

        failedQuests.Remove(questId);

        QuestRuntimeData runtimeData = QuestRuntimeFactory.CreateFromData(questData);
        if (runtimeData == null)
            return false;

        bool wasAutoPinned = false;

        if (autoPinAcceptedQuests && GetPinnedQuestCount() < MaxPinnedQuests)
        {
            runtimeData.IsPinned = true;
            wasAutoPinned = true;
        }

        activeQuests[questId] = runtimeData;

        OnQuestAccepted?.Invoke(questData);

        if (wasAutoPinned)
        {
            OnPinnedQuestsChanged?.Invoke();
        }

        OnQuestListChanged?.Invoke();

        RefreshItemObjectivesForQuest(questId);
        TryAdvanceZeroObjectiveSteps(questId);

        return true;
    }

    public bool AdvanceQuestStep(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        if (runtimeData.QuestState != QuestState.Active)
            return false;

        QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
        if (currentStep == null)
            return false;

        currentStep.StepState = QuestStepState.Completed;

        int nextStepIndex = runtimeData.CurrentStepIndex + 1;

        if (nextStepIndex >= runtimeData.Steps.Count)
        {
            runtimeData.QuestState = QuestState.ReadyToTurnIn;

            QuestData questData = GetQuestData(questId);
            if (questData != null)
            {
                OnQuestReadyToTurnIn?.Invoke(questData);
            }

            OnQuestListChanged?.Invoke();
            return true;
        }

        runtimeData.CurrentStepIndex = nextStepIndex;
        runtimeData.Steps[nextStepIndex].StepState = QuestStepState.Active;

        QuestData advancedQuestData = GetQuestData(questId);
        if (advancedQuestData != null)
        {
            OnQuestAdvanced?.Invoke(advancedQuestData);
        }

        OnQuestListChanged?.Invoke();

        RefreshItemObjectivesForQuest(questId);
        TryAdvanceZeroObjectiveSteps(questId);

        return true;
    }

    public bool SetQuestReadyToTurnIn(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        if (runtimeData.QuestState != QuestState.Active)
            return false;

        runtimeData.QuestState = QuestState.ReadyToTurnIn;

        QuestData questData = GetQuestData(questId);
        if (questData != null)
        {
            OnQuestReadyToTurnIn?.Invoke(questData);
        }

        OnQuestListChanged?.Invoke();
        return true;
    }

    public bool CompleteQuest(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        if (runtimeData.QuestState != QuestState.ReadyToTurnIn &&
            runtimeData.QuestState != QuestState.Active)
            return false;

        runtimeData.QuestState = QuestState.Completed;
        runtimeData.IsPinned = false;

        activeQuests.Remove(questId);
        completedQuests[questId] = runtimeData;

        QuestData questData = GetQuestData(questId);
        if (questData != null)
        {
            OnQuestCompleted?.Invoke(questData);
        }

        OnQuestListChanged?.Invoke();
        OnPinnedQuestsChanged?.Invoke();
        return true;
    }

    public bool FailQuest(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        runtimeData.QuestState = QuestState.Failed;
        runtimeData.IsPinned = false;

        QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
        if (currentStep != null)
        {
            currentStep.StepState = QuestStepState.Failed;
        }

        activeQuests.Remove(questId);
        failedQuests[questId] = runtimeData;

        QuestData questData = GetQuestData(questId);
        if (questData != null)
        {
            OnQuestFailed?.Invoke(questData);
        }

        OnQuestListChanged?.Invoke();
        OnPinnedQuestsChanged?.Invoke();
        return true;
    }

    public bool IsQuestPinned(string questId)
    {
        QuestRuntimeData runtime = GetQuestRuntime(questId);
        return runtime != null && runtime.IsPinned;
    }

    public int GetPinnedQuestCount()
    {
        int count = 0;

        foreach (QuestRuntimeData runtime in activeQuests.Values)
        {
            if (runtime.IsPinned)
            {
                count++;
            }
        }

        return count;
    }

    public bool PinQuest(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        if (runtimeData.IsPinned)
            return true;

        if (GetPinnedQuestCount() >= MaxPinnedQuests)
            return false;

        runtimeData.IsPinned = true;
        OnPinnedQuestsChanged?.Invoke();
        OnQuestListChanged?.Invoke();
        return true;
    }

    public bool UnpinQuest(string questId)
    {
        QuestRuntimeData runtimeData = GetQuestRuntime(questId);
        if (runtimeData == null)
            return false;

        if (!runtimeData.IsPinned)
            return true;

        runtimeData.IsPinned = false;
        OnPinnedQuestsChanged?.Invoke();
        OnQuestListChanged?.Invoke();
        return true;
    }

    public List<QuestRuntimeData> GetPinnedActiveQuests()
    {
        List<QuestRuntimeData> pinned = new();

        foreach (QuestRuntimeData runtime in activeQuests.Values)
        {
            if (runtime.IsPinned)
            {
                pinned.Add(runtime);
            }
        }

        return pinned;
    }

    public bool HandleQuestAction(string questId, QuestActionType actionType)
    {
        switch (actionType)
        {
            case QuestActionType.AcceptQuest:
                return AcceptQuest(questId);

            case QuestActionType.AdvanceQuestStep:
                return AdvanceQuestStep(questId);

            case QuestActionType.CompleteQuest:
                return CompleteQuest(questId);

            case QuestActionType.FailQuest:
                return FailQuest(questId);

            case QuestActionType.None:
            default:
                return false;
        }
    }

    public bool AcceptQuestObjective(string questId, string objectiveId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId))
            return false;

        if (amount <= 0)
            amount = 1;

        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return false;

        if (runtimeData.QuestState != QuestState.Active)
            return false;

        QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
        if (currentStep == null || currentStep.Objectives == null)
            return false;

        bool anyProgress = false;

        for (int i = 0; i < currentStep.Objectives.Count; i++)
        {
            QuestObjectiveRuntimeData objective = currentStep.Objectives[i];

            if (objective == null)
                continue;

            if (objective.IsCompleted || objective.IsFailed)
                continue;

            if (!string.Equals(objective.ObjectiveId, objectiveId, StringComparison.Ordinal))
                continue;

            objective.CurrentAmount += amount;

            if (objective.CurrentAmount >= objective.RequiredAmount)
            {
                objective.CurrentAmount = objective.RequiredAmount;
                objective.IsCompleted = true;
            }

            anyProgress = true;
            break;
        }

        if (!anyProgress)
            return false;

        QuestData questData = GetQuestData(questId);
        if (questData != null)
        {
            OnQuestProgressChanged?.Invoke(questData);
        }

        OnQuestListChanged?.Invoke();

        if (currentStep.AreAllObjectivesCompleted())
        {
            AdvanceQuestStep(questId);
        }

        return true;
    }

    public void NotifyNpcTalked(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return;

        List<string> questIds = GetActiveQuestIdsSnapshot();

        for (int i = 0; i < questIds.Count; i++)
        {
            TryProgressObjectiveByTarget(questIds[i], QuestObjectiveType.TalkToNpc, npcId, 1);
        }
    }

    public void NotifyTriggerZoneReached(string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            return;

        List<string> questIds = GetActiveQuestIdsSnapshot();

        for (int i = 0; i < questIds.Count; i++)
        {
            TryProgressObjectiveByTarget(questIds[i], QuestObjectiveType.ReachTriggerZone, zoneId, 1);
        }
    }

    public void NotifyEnemyKilled(string enemyTypeId, string enemyUniqueId)
    {
        List<string> questIds = GetActiveQuestIdsSnapshot();

        for (int i = 0; i < questIds.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(enemyTypeId))
            {
                TryProgressObjectiveByTarget(questIds[i], QuestObjectiveType.KillEnemyType, enemyTypeId, 1);
            }

            if (!string.IsNullOrWhiteSpace(enemyUniqueId))
            {
                TryProgressObjectiveByTarget(questIds[i], QuestObjectiveType.KillSpecificEnemy, enemyUniqueId, 1);
            }
        }
    }

    private void HandleLevelChanged(int newLevel)
    {
        TryAutoStartQuestsByCurrentLevel();
        OnQuestListChanged?.Invoke();
    }

    private void HandleInventoryChanged()
    {
        TryAutoStartQuestsByInventory();
        RefreshItemObjectivesForAllActiveQuests();
        OnQuestListChanged?.Invoke();
    }

    private void HandleQuestCompletedForProgressAndAutoStart(QuestData completedQuestData)
    {
        if (completedQuestData == null)
            return;

        List<string> questIds = GetActiveQuestIdsSnapshot();

        for (int i = 0; i < questIds.Count; i++)
        {
            TryProgressObjectiveByTarget(questIds[i], QuestObjectiveType.CompleteQuest, completedQuestData.QuestId, 1);
        }

        TryAutoStartQuestsByCompletedQuest(completedQuestData.QuestId);
        OnQuestListChanged?.Invoke();
    }

    private bool AreQuestAvailabilityConditionsMet(QuestData questData)
    {
        if (questData == null)
            return false;

        if (questData.RequiredPlayerLevel > 0)
        {
            int currentLevel = expSystem != null ? expSystem.CurrentLvl : 0;
            if (currentLevel < questData.RequiredPlayerLevel)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(questData.RequiredItemId))
        {
            if (!InventoryContainsItemId(questData.RequiredItemId))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(questData.RequiredCompletedQuestId))
        {
            if (GetQuestState(questData.RequiredCompletedQuestId) != QuestState.Completed)
                return false;
        }

        return true;
    }

    private void TryAutoStartQuestsByCurrentLevel()
    {
        foreach (QuestData questData in questDataById.Values)
        {
            if (questData == null)
                continue;

            if (questData.StartType != QuestStartType.PlayerLevelReached)
                continue;

            if (!AreQuestAvailabilityConditionsMet(questData))
                continue;

            AcceptQuest(questData.QuestId);
        }
    }

    private void TryAutoStartQuestsByInventory()
    {
        foreach (QuestData questData in questDataById.Values)
        {
            if (questData == null)
                continue;

            if (questData.StartType != QuestStartType.ItemReceived)
                continue;

            if (!AreQuestAvailabilityConditionsMet(questData))
                continue;

            AcceptQuest(questData.QuestId);
        }
    }

    private void TryAutoStartQuestsByCompletedQuest(string completedQuestId)
    {
        if (string.IsNullOrWhiteSpace(completedQuestId))
            return;

        foreach (QuestData questData in questDataById.Values)
        {
            if (questData == null)
                continue;

            if (questData.StartType != QuestStartType.QuestCompleted)
                continue;

            if (!string.Equals(questData.RequiredCompletedQuestId, completedQuestId, StringComparison.Ordinal))
                continue;

            if (!AreQuestAvailabilityConditionsMet(questData))
                continue;

            AcceptQuest(questData.QuestId);
        }
    }

    private void TryAutoStartQuestsByCompletedQuestState()
    {
        foreach (QuestData questData in questDataById.Values)
        {
            if (questData == null)
                continue;

            if (questData.StartType != QuestStartType.QuestCompleted)
                continue;

            if (!AreQuestAvailabilityConditionsMet(questData))
                continue;

            AcceptQuest(questData.QuestId);
        }
    }

    private bool InventoryContainsItemId(string itemId)
    {
        return GetInventoryItemCountByItemId(itemId) > 0;
    }

    private int GetInventoryItemCountByItemId(string itemId)
    {
        if (InventorySystem.Instance == null || string.IsNullOrWhiteSpace(itemId))
            return 0;

        int total = 0;
        total += CountItemIdInList(InventorySystem.Instance.ConsumableItems, itemId);
        total += CountItemIdInList(InventorySystem.Instance.QuestItems, itemId);
        total += CountItemIdInList(InventorySystem.Instance.EquipmentItems, itemId);
        return total;
    }

    private int CountItemIdInList(IReadOnlyList<InventoryEntry> entries, string itemId)
    {
        if (entries == null)
            return 0;

        int count = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            InventoryEntry entry = entries[i];

            if (entry == null || entry.Item == null)
                continue;

            if (entry.Amount <= 0)
                continue;

            if (entry.Item.ItemId == itemId)
            {
                count += entry.Amount;
            }
        }

        return count;
    }

    private List<string> GetActiveQuestIdsSnapshot()
    {
        List<string> ids = new();

        foreach (KeyValuePair<string, QuestRuntimeData> pair in activeQuests)
        {
            ids.Add(pair.Key);
        }

        return ids;
    }

    private void RefreshItemObjectivesForAllActiveQuests()
    {
        List<string> questIds = GetActiveQuestIdsSnapshot();

        for (int i = 0; i < questIds.Count; i++)
        {
            RefreshItemObjectivesForQuest(questIds[i]);
        }
    }

    private void RefreshItemObjectivesForQuest(string questId)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return;

        if (runtimeData.QuestState != QuestState.Active)
            return;

        QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
        if (currentStep == null || currentStep.Objectives == null)
            return;

        bool anyProgress = false;

        for (int i = 0; i < currentStep.Objectives.Count; i++)
        {
            QuestObjectiveRuntimeData objective = currentStep.Objectives[i];

            if (objective == null)
                continue;

            if (objective.IsCompleted || objective.IsFailed)
                continue;

            if (objective.ObjectiveType != QuestObjectiveType.ObtainItem)
                continue;

            int itemCount = GetInventoryItemCountByItemId(objective.TargetId);
            int clamped = Mathf.Clamp(itemCount, 0, objective.RequiredAmount);

            if (clamped != objective.CurrentAmount)
            {
                objective.CurrentAmount = clamped;
                anyProgress = true;
            }

            if (objective.CurrentAmount >= objective.RequiredAmount)
            {
                objective.CurrentAmount = objective.RequiredAmount;
                objective.IsCompleted = true;
                anyProgress = true;
            }
        }

        if (!anyProgress)
            return;

        QuestData questData = GetQuestData(questId);
        if (questData != null)
        {
            OnQuestProgressChanged?.Invoke(questData);
        }

        OnQuestListChanged?.Invoke();

        if (currentStep.AreAllObjectivesCompleted())
        {
            AdvanceQuestStep(questId);
        }
    }

    private void TryAdvanceZeroObjectiveSteps(string questId)
    {
        int safety = 50;

        while (safety-- > 0)
        {
            if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
                return;

            if (runtimeData.QuestState != QuestState.Active)
                return;

            QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
            if (currentStep == null)
                return;

            if (currentStep.Objectives != null && currentStep.Objectives.Count > 0)
                return;

            bool advanced = AdvanceQuestStep(questId);
            if (!advanced)
                return;
        }

        Debug.LogWarning($"QuestManager: safety break triggered while auto-advancing zero-objective steps for quest '{questId}'.");
    }

    private void TryProgressObjectiveByTarget(string questId, QuestObjectiveType objectiveType, string targetId, int amount)
    {
        if (!activeQuests.TryGetValue(questId, out QuestRuntimeData runtimeData))
            return;

        if (runtimeData.QuestState != QuestState.Active)
            return;

        QuestStepRuntimeData currentStep = runtimeData.GetCurrentStep();
        if (currentStep == null || currentStep.Objectives == null)
            return;

        bool anyProgress = false;

        for (int i = 0; i < currentStep.Objectives.Count; i++)
        {
            QuestObjectiveRuntimeData objective = currentStep.Objectives[i];

            if (objective == null)
                continue;

            if (objective.IsCompleted || objective.IsFailed)
                continue;

            if (objective.ObjectiveType != objectiveType)
                continue;

            if (!string.Equals(objective.TargetId, targetId, StringComparison.Ordinal))
                continue;

            objective.CurrentAmount += amount;

            if (objective.CurrentAmount >= objective.RequiredAmount)
            {
                objective.CurrentAmount = objective.RequiredAmount;
                objective.IsCompleted = true;
            }

            anyProgress = true;
        }

        if (!anyProgress)
            return;

        QuestData questData = GetQuestData(questId);
        if (questData != null)
        {
            OnQuestProgressChanged?.Invoke(questData);
        }

        OnQuestListChanged?.Invoke();

        if (currentStep.AreAllObjectivesCompleted())
        {
            AdvanceQuestStep(questId);
        }
    }
}