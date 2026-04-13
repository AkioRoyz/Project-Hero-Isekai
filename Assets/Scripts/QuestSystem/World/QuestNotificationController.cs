using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public class QuestNotificationController : MonoBehaviour
{
    private class NotificationRequest
    {
        public QuestNotificationUI.NotificationType Type;
        public bool UseLocalizedTitle;
        public string PlainTitle;
        public LocalizedString LocalizedTitle;
        public string FallbackTitle;
    }

    [Header("UI")]
    [SerializeField] private QuestNotificationUI notificationUI;

    private readonly Queue<NotificationRequest> queue = new();
    private Coroutine queueRoutine;

    private bool isSubscribedToQuestManager;
    private bool isSubscribedToGameState;

    private void Awake()
    {
        if (notificationUI == null)
        {
            notificationUI = GetComponentInChildren<QuestNotificationUI>(true);
        }

        if (notificationUI != null)
        {
            notificationUI.HideImmediate();
        }
    }

    private void OnEnable()
    {
        TrySubscribeQuestManager();
        TrySubscribeGameStateManager();
    }

    private void Start()
    {
        TrySubscribeQuestManager();
        TrySubscribeGameStateManager();

        if (notificationUI != null)
        {
            notificationUI.HideImmediate();
        }
    }

    private void OnDisable()
    {
        UnsubscribeQuestManager();
        UnsubscribeGameStateManager();

        if (queueRoutine != null)
        {
            StopCoroutine(queueRoutine);
            queueRoutine = null;
        }
    }

    private void TrySubscribeQuestManager()
    {
        if (isSubscribedToQuestManager)
            return;

        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestAccepted += HandleQuestAccepted;
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        isSubscribedToQuestManager = true;
    }

    private void UnsubscribeQuestManager()
    {
        if (!isSubscribedToQuestManager)
            return;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= HandleQuestAccepted;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }

        isSubscribedToQuestManager = false;
    }

    private void TrySubscribeGameStateManager()
    {
        if (isSubscribedToGameState)
            return;

        if (GameStateManager.Instance == null)
            return;

        GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        isSubscribedToGameState = true;
    }

    private void UnsubscribeGameStateManager()
    {
        if (!isSubscribedToGameState)
            return;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        isSubscribedToGameState = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState != GameState.Playing)
            return;

        TryStartQueueRoutine();
    }

    private void HandleQuestAccepted(QuestData questData)
    {
        if (questData == null || !questData.NotifyOnAccept)
            return;

        EnqueueLocalized(
            QuestNotificationUI.NotificationType.Accepted,
            questData.QuestTitle,
            questData.QuestId);
    }

    private void HandleQuestCompleted(QuestData questData)
    {
        if (questData == null || !questData.NotifyOnComplete)
            return;

        EnqueueLocalized(
            QuestNotificationUI.NotificationType.Completed,
            questData.QuestTitle,
            questData.QuestId);
    }

    public void EnqueuePlain(QuestNotificationUI.NotificationType type, string questTitle)
    {
        if (notificationUI == null)
            return;

        queue.Enqueue(new NotificationRequest
        {
            Type = type,
            UseLocalizedTitle = false,
            PlainTitle = questTitle ?? string.Empty,
            FallbackTitle = questTitle ?? string.Empty
        });

        TryStartQueueRoutine();
    }

    public void EnqueueLocalized(QuestNotificationUI.NotificationType type, LocalizedString localizedTitle, string fallbackTitle = "")
    {
        if (notificationUI == null)
            return;

        queue.Enqueue(new NotificationRequest
        {
            Type = type,
            UseLocalizedTitle = true,
            LocalizedTitle = localizedTitle,
            FallbackTitle = fallbackTitle ?? string.Empty
        });

        TryStartQueueRoutine();
    }

    public void QueueAccepted(string questTitle) =>
        EnqueuePlain(QuestNotificationUI.NotificationType.Accepted, questTitle);

    public void QueueCompleted(string questTitle) =>
        EnqueuePlain(QuestNotificationUI.NotificationType.Completed, questTitle);

    public void QueueAccepted(LocalizedString localizedTitle, string fallbackTitle = "") =>
        EnqueueLocalized(QuestNotificationUI.NotificationType.Accepted, localizedTitle, fallbackTitle);

    public void QueueCompleted(LocalizedString localizedTitle, string fallbackTitle = "") =>
        EnqueueLocalized(QuestNotificationUI.NotificationType.Completed, localizedTitle, fallbackTitle);

    private void TryStartQueueRoutine()
    {
        if (queueRoutine != null)
            return;

        if (queue.Count == 0)
            return;

        if (!IsGameInPlayingState())
            return;

        queueRoutine = StartCoroutine(ProcessQueueRoutine());
    }

    private IEnumerator ProcessQueueRoutine()
    {
        while (queue.Count > 0)
        {
            yield return WaitUntilPlayingState();

            NotificationRequest request = queue.Dequeue();

            if (notificationUI != null)
            {
                if (request.UseLocalizedTitle)
                {
                    notificationUI.Show(request.Type, request.LocalizedTitle, request.FallbackTitle);
                }
                else
                {
                    notificationUI.Show(request.Type, request.PlainTitle);
                }

                float duration = notificationUI.GetTotalDisplayDuration();
                if (duration > 0f)
                    yield return WaitUnscaled(duration + 0.05f);
                else
                    yield return null;
            }
            else
            {
                yield return null;
            }
        }

        queueRoutine = null;

        if (queue.Count > 0 && IsGameInPlayingState())
        {
            queueRoutine = StartCoroutine(ProcessQueueRoutine());
        }
    }

    private IEnumerator WaitUntilPlayingState()
    {
        while (!IsGameInPlayingState())
        {
            yield return null;
        }
    }

    private bool IsGameInPlayingState()
    {
        if (GameStateManager.Instance == null)
            return true;

        return GameStateManager.Instance.CurrentState == GameState.Playing;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}