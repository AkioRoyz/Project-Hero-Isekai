using System.Collections.Generic;
using UnityEngine;

public class QuestMarkerSource : MonoBehaviour
{
    private struct MarkerResult
    {
        public QuestMarkerState State;
        public QuestType QuestType;
        public bool IsValid;

        public MarkerResult(QuestMarkerState state, QuestType questType, bool isValid)
        {
            State = state;
            QuestType = questType;
            IsValid = isValid;
        }
    }

    [Header("Marker UI")]
    [SerializeField] private QuestMarkerDisplayUI markerDisplayUI;

    [Header("Linked Quests")]
    [SerializeField] private List<QuestMarkerLinkData> linkedQuests = new();

    [Header("Behaviour")]
    [SerializeField] private bool refreshOnEnable = true;

    private bool isSubscribed;

    private void OnEnable()
    {
        TrySubscribeQuestManager();

        if (refreshOnEnable)
        {
            RefreshMarker();
        }
    }

    private void Start()
    {
        TrySubscribeQuestManager();
        RefreshMarker();
    }

    private void OnDisable()
    {
        UnsubscribeQuestManager();
    }

    [ContextMenu("Force Refresh Marker")]
    public void RefreshMarker()
    {
        TrySubscribeQuestManager();

        if (markerDisplayUI == null)
            return;

        if (QuestManager.Instance == null)
        {
            markerDisplayUI.Hide();
            return;
        }

        MarkerResult result = EvaluateMarker();

        if (!result.IsValid || result.State == QuestMarkerState.None)
        {
            markerDisplayUI.Hide();
            return;
        }

        markerDisplayUI.Show(result.State, result.QuestType);
    }

    private MarkerResult EvaluateMarker()
    {
        MarkerResult best = new MarkerResult(QuestMarkerState.None, QuestType.Side, false);

        for (int i = 0; i < linkedQuests.Count; i++)
        {
            QuestMarkerLinkData link = linkedQuests[i];
            if (link == null)
                continue;

            if (string.IsNullOrWhiteSpace(link.QuestId))
                continue;

            QuestData questData = QuestManager.Instance.GetQuestData(link.QuestId);
            if (questData == null)
                continue;

            QuestMarkerState state = EvaluateQuestState(link, questData);

            if (state == QuestMarkerState.None)
                continue;

            MarkerResult candidate = new MarkerResult(state, questData.QuestType, true);

            if (!best.IsValid || GetPriority(candidate.State) > GetPriority(best.State))
            {
                best = candidate;
            }
        }

        return best;
    }

    private QuestMarkerState EvaluateQuestState(QuestMarkerLinkData link, QuestData questData)
    {
        if (link == null || questData == null || QuestManager.Instance == null)
            return QuestMarkerState.None;

        QuestState questState = QuestManager.Instance.GetQuestState(link.QuestId);

        if (questState == QuestState.ReadyToTurnIn && link.ShowReadyToTurnInMarker)
        {
            return QuestMarkerState.ReadyToTurnIn;
        }

        if (questState == QuestState.NotStarted)
        {
            if (questData.HiddenUntilStarted)
                return QuestMarkerState.None;

            if (link.ShowAvailableMarker && QuestManager.Instance.CanQuestBeOfferedNow(link.QuestId))
            {
                return QuestMarkerState.Available;
            }
        }

        if (questState == QuestState.Active && link.ShowInProgressMarker)
        {
            return QuestMarkerState.InProgress;
        }

        if (questState == QuestState.Failed)
        {
            if (questData.HiddenUntilStarted)
                return QuestMarkerState.None;

            if (link.ShowAvailableMarker && QuestManager.Instance.CanQuestBeOfferedNow(link.QuestId))
            {
                return QuestMarkerState.Available;
            }
        }

        if (questState == QuestState.Completed)
        {
            if (questData.HiddenUntilStarted)
                return QuestMarkerState.None;

            if (link.ShowAvailableMarker && QuestManager.Instance.CanQuestBeOfferedNow(link.QuestId))
            {
                return QuestMarkerState.Available;
            }
        }

        return QuestMarkerState.None;
    }

    private int GetPriority(QuestMarkerState state)
    {
        switch (state)
        {
            case QuestMarkerState.ReadyToTurnIn:
                return 300;

            case QuestMarkerState.Available:
                return 200;

            case QuestMarkerState.InProgress:
                return 100;

            case QuestMarkerState.None:
            default:
                return 0;
        }
    }

    private void TrySubscribeQuestManager()
    {
        if (isSubscribed)
            return;

        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestListChanged += HandleQuestListChanged;
        isSubscribed = true;
    }

    private void UnsubscribeQuestManager()
    {
        if (!isSubscribed)
            return;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListChanged -= HandleQuestListChanged;
        }

        isSubscribed = false;
    }

    private void HandleQuestListChanged()
    {
        RefreshMarker();
    }
}