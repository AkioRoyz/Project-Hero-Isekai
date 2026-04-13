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

    private Coroutine currentRoutine;
    private int showRequestVersion;

    private void Awake()
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

        if (requestVersion != showRequestVersion)
            yield break;

        yield return FadeCanvasGroup(0f, 1f, fadeInDuration);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return WaitUnscaled(visibleDuration);

        if (requestVersion != showRequestVersion)
            yield break;

        yield return FadeCanvasGroup(1f, 0f, fadeOutDuration);

        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (requestVersion == showRequestVersion)
            currentRoutine = null;
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