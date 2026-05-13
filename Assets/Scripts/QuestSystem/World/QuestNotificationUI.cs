using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class QuestNotificationUI : MonoBehaviour
{
    public enum NotificationType
    {
        Accepted,
        Completed
    }

    [Serializable]
    private class TextBlinkSettings
    {
        [SerializeField] private bool enabled = true;

        [Tooltip("Задержка перед началом мигания.")]
        [SerializeField][Min(0f)] private float delay = 0.05f;

        [Tooltip("Общая длительность мигания.")]
        [SerializeField][Min(0.01f)] private float duration = 0.75f;

        [Tooltip("Количество миганий за Duration.")]
        [SerializeField][Min(1)] private int blinkCount = 3;

        [Tooltip("Минимальная прозрачность текста во время мигания.")]
        [SerializeField][Range(0f, 1f)] private float minAlpha = 0.15f;

        [Tooltip("Максимальная прозрачность текста во время мигания.")]
        [SerializeField][Range(0f, 1f)] private float maxAlpha = 1f;

        public bool Enabled => enabled;
        public float Delay => Mathf.Max(0f, delay);
        public float Duration => Mathf.Max(0.01f, duration);
        public int BlinkCount => Mathf.Max(1, blinkCount);

        public float MinAlpha => Mathf.Clamp01(Mathf.Min(minAlpha, maxAlpha));
        public float MaxAlpha => Mathf.Clamp01(Mathf.Max(minAlpha, maxAlpha));
    }

    [Header("Visual Root")]
    [SerializeField] private GameObject visualRoot;

    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI questTitleText;

    [Header("Localized Headers")]
    [SerializeField] private LocalizedString acceptedHeaderText;
    [SerializeField] private LocalizedString completedHeaderText;

    [Header("Animation")]
    [SerializeField] private float delayBeforeShow = 0.2f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float visibleDuration = 2.25f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Blink On Appear")]
    [SerializeField] private TextBlinkSettings headerBlink = new TextBlinkSettings();
    [SerializeField] private TextBlinkSettings questTitleBlink = new TextBlinkSettings();

    private Coroutine currentRoutine;

    private Coroutine headerBlinkRoutine;
    private Coroutine questTitleBlinkRoutine;

    private Color headerColorBeforeBlink;
    private Color questTitleColorBeforeBlink;

    private bool hasHeaderColorBeforeBlink;
    private bool hasQuestTitleColorBeforeBlink;

    private int showRequestVersion;

    private void Awake()
    {
        HideImmediate();
    }

    private void OnDisable()
    {
        HideImmediate();
    }

    public void ShowAccepted(string questTitle)
    {
        Show(NotificationType.Accepted, questTitle);
    }

    public void ShowCompleted(string questTitle)
    {
        Show(NotificationType.Completed, questTitle);
    }

    public void ShowAccepted(LocalizedString localizedQuestTitle, string fallback = "")
    {
        Show(NotificationType.Accepted, localizedQuestTitle, fallback);
    }

    public void ShowCompleted(LocalizedString localizedQuestTitle, string fallback = "")
    {
        Show(NotificationType.Completed, localizedQuestTitle, fallback);
    }

    public void Show(NotificationType type, string questTitle)
    {
        if (!gameObject.activeInHierarchy)
            return;

        showRequestVersion++;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = null;

        StopBlinkRoutines(true);

        currentRoutine = StartCoroutine(
            ShowRoutineWithPlainTitle(type, questTitle ?? string.Empty, showRequestVersion));
    }

    public void Show(NotificationType type, LocalizedString localizedQuestTitle, string fallback = "")
    {
        if (!gameObject.activeInHierarchy)
            return;

        showRequestVersion++;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = null;

        StopBlinkRoutines(true);

        currentRoutine = StartCoroutine(
            ShowRoutineWithLocalizedTitle(type, localizedQuestTitle, fallback ?? string.Empty, showRequestVersion));
    }

    public float GetTotalDisplayDuration()
    {
        return delayBeforeShow + fadeInDuration + visibleDuration + fadeOutDuration;
    }

    public void HideImmediate()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        StopBlinkRoutines(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private IEnumerator ShowRoutineWithPlainTitle(NotificationType type, string questTitle, int requestVersion)
    {
        yield return LocalizationSettings.InitializationOperation;

        if (requestVersion != showRequestVersion)
            yield break;

        LocalizedString headerLocalized =
            type == NotificationType.Accepted ? acceptedHeaderText : completedHeaderText;

        string headerFallback =
            type == NotificationType.Accepted ? "Новый квест" : "Квест завершён";

        string headerResult = null;
        yield return GetLocalizedStringCoroutine(
            headerLocalized,
            headerFallback,
            result => headerResult = result);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return RunVisualSequence(headerResult, questTitle ?? string.Empty, requestVersion);
    }

    private IEnumerator ShowRoutineWithLocalizedTitle(NotificationType type, LocalizedString localizedQuestTitle, string fallback, int requestVersion)
    {
        yield return LocalizationSettings.InitializationOperation;

        if (requestVersion != showRequestVersion)
            yield break;

        LocalizedString headerLocalized =
            type == NotificationType.Accepted ? acceptedHeaderText : completedHeaderText;

        string headerFallback =
            type == NotificationType.Accepted ? "Новый квест" : "Квест завершён";

        string headerResult = null;
        yield return GetLocalizedStringCoroutine(
            headerLocalized,
            headerFallback,
            result => headerResult = result);

        if (requestVersion != showRequestVersion)
            yield break;

        string questTitleResult = null;
        yield return GetLocalizedStringCoroutine(
            localizedQuestTitle,
            fallback,
            result => questTitleResult = result);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return RunVisualSequence(headerResult, questTitleResult, requestVersion);
    }

    private IEnumerator RunVisualSequence(string header, string questTitle, int requestVersion)
    {
        yield return WaitUnscaled(delayBeforeShow);

        if (requestVersion != showRequestVersion)
            yield break;

        StopBlinkRoutines(true);

        if (headerText != null)
            headerText.text = header ?? string.Empty;

        if (questTitleText != null)
            questTitleText.text = questTitle ?? string.Empty;

        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        StartBlinkRoutines(requestVersion);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return FadeCanvasGroup(0f, 1f, fadeInDuration);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return WaitUnscaled(visibleDuration);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return FadeCanvasGroup(1f, 0f, fadeOutDuration);

        StopBlinkRoutines(true);

        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (requestVersion == showRequestVersion)
            currentRoutine = null;
    }

    private void StartBlinkRoutines(int requestVersion)
    {
        StartTextBlink(headerText, headerBlink, true, requestVersion);
        StartTextBlink(questTitleText, questTitleBlink, false, requestVersion);
    }

    private void StartTextBlink(TextMeshProUGUI targetText, TextBlinkSettings settings, bool isHeader, int requestVersion)
    {
        if (targetText == null)
            return;

        if (settings == null || !settings.Enabled)
            return;

        if (isHeader)
        {
            headerColorBeforeBlink = targetText.color;
            hasHeaderColorBeforeBlink = true;

            if (headerBlinkRoutine != null)
                StopCoroutine(headerBlinkRoutine);

            headerBlinkRoutine = StartCoroutine(BlinkTextRoutine(targetText, settings, true, requestVersion));
        }
        else
        {
            questTitleColorBeforeBlink = targetText.color;
            hasQuestTitleColorBeforeBlink = true;

            if (questTitleBlinkRoutine != null)
                StopCoroutine(questTitleBlinkRoutine);

            questTitleBlinkRoutine = StartCoroutine(BlinkTextRoutine(targetText, settings, false, requestVersion));
        }
    }

    private IEnumerator BlinkTextRoutine(TextMeshProUGUI targetText, TextBlinkSettings settings, bool isHeader, int requestVersion)
    {
        if (targetText == null || settings == null)
            yield break;

        if (settings.Delay > 0f)
        {
            float delayTimer = 0f;

            while (delayTimer < settings.Delay)
            {
                if (requestVersion != showRequestVersion)
                {
                    RestoreBlinkColor(isHeader);
                    ClearBlinkRoutineReference(isHeader);
                    yield break;
                }

                delayTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        float timer = 0f;
        float duration = settings.Duration;
        int blinkCount = settings.BlinkCount;

        while (timer < duration)
        {
            if (requestVersion != showRequestVersion)
            {
                RestoreBlinkColor(isHeader);
                ClearBlinkRoutineReference(isHeader);
                yield break;
            }

            float normalizedTime = Mathf.Clamp01(timer / duration);
            float wave = Mathf.PingPong(normalizedTime * blinkCount * 2f, 1f);

            float alpha = Mathf.Lerp(settings.MaxAlpha, settings.MinAlpha, wave);
            SetTextAlpha(targetText, alpha);

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreBlinkColor(isHeader);
        ClearBlinkRoutineReference(isHeader);
    }

    private void StopBlinkRoutines(bool restoreTextColors)
    {
        if (headerBlinkRoutine != null)
        {
            StopCoroutine(headerBlinkRoutine);
            headerBlinkRoutine = null;
        }

        if (questTitleBlinkRoutine != null)
        {
            StopCoroutine(questTitleBlinkRoutine);
            questTitleBlinkRoutine = null;
        }

        if (restoreTextColors)
        {
            RestoreBlinkColor(true);
            RestoreBlinkColor(false);
        }
    }

    private void RestoreBlinkColor(bool isHeader)
    {
        if (isHeader)
        {
            if (hasHeaderColorBeforeBlink && headerText != null)
                headerText.color = headerColorBeforeBlink;

            hasHeaderColorBeforeBlink = false;
        }
        else
        {
            if (hasQuestTitleColorBeforeBlink && questTitleText != null)
                questTitleText.color = questTitleColorBeforeBlink;

            hasQuestTitleColorBeforeBlink = false;
        }
    }

    private void ClearBlinkRoutineReference(bool isHeader)
    {
        if (isHeader)
            headerBlinkRoutine = null;
        else
            questTitleBlinkRoutine = null;
    }

    private static void SetTextAlpha(TextMeshProUGUI targetText, float alpha)
    {
        if (targetText == null)
            return;

        Color color = targetText.color;
        color.a = Mathf.Clamp01(alpha);
        targetText.color = color;
    }

    private IEnumerator GetLocalizedStringCoroutine(LocalizedString localizedString, string fallback, Action<string> onComplete)
    {
        if (localizedString == null || localizedString.IsEmpty)
        {
            onComplete?.Invoke(fallback ?? string.Empty);
            yield break;
        }

        AsyncOperationHandle<string> handle = localizedString.GetLocalizedStringAsync();
        yield return handle;

        string result = null;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            result = handle.Result;

        onComplete?.Invoke(!string.IsNullOrEmpty(result) ? result : (fallback ?? string.Empty));
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;
        canvasGroup.alpha = from;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}