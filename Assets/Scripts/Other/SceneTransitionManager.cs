using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private CanvasGroup screenRootGroup;
    [SerializeField] private CanvasGroup loadingContentGroup;
    [SerializeField] private Image blackOverlay;

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text tipText;

    [Header("Random Content")]
    [SerializeField] private Sprite[] randomBackgrounds;
    [SerializeField] private LocalizedString[] randomTips;

    [Header("Timing")]
    [SerializeField][Min(0f)] private float fadeFromBlackDuration = 0.35f;
    [SerializeField][Min(0f)] private float fadeToBlackDuration = 0.35f;
    [SerializeField][Min(0f)] private float fadeIntoSceneDuration = 0.35f;
    [SerializeField][Min(0f)] private float minimumVisibleLoadingTime = 1.0f;
    [SerializeField][Min(0f)] private float holdBlackAfterSceneActivation = 0.05f;
    [SerializeField][Min(0f)] private float progressSmoothingSpeed = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool showLogs = true;

    public bool IsLoading => isLoading;

    private bool isLoading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SceneTransitionManager] Duplicate instance destroyed: {name}", this);
            Destroy(this);
            return;
        }

        Instance = this;
        HideScreenInstant();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, null);
    }

    public void LoadScene(string sceneName, string entryPointId)
    {
        if (isLoading)
        {
            if (showLogs)
                Debug.LogWarning("[SceneTransitionManager] Transition already in progress.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] Scene name is empty.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneTransitionManager] Scene '{sceneName}' is not in Build Settings.", this);
            return;
        }

        if (screenRootGroup == null || loadingContentGroup == null || blackOverlay == null)
        {
            Debug.LogError("[SceneTransitionManager] UI references are not assigned.", this);
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, entryPointId));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string entryPointId)
    {
        isLoading = true;

        if (showLogs)
            Debug.Log($"[SceneTransitionManager] Loading scene: {sceneName}, entryPointId = {entryPointId}", this);

        SceneTransitionState.SetNextEntryPoint(entryPointId);

        ShowScreenInstant();
        ApplyRandomBackground();
        StartCoroutine(ApplyRandomTipRoutine());
        ResetProgressUI();

        loadingContentGroup.alpha = 1f;
        SetBlackOverlayAlpha(1f);

        yield return null;

        // Появляемся из темноты: чёрный экран уходит, открывая loading screen.
        yield return FadeBlackOverlay(1f, 0f, fadeFromBlackDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogError($"[SceneTransitionManager] Failed to start async load for scene '{sceneName}'.", this);
            HideScreenInstant();
            isLoading = false;
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        float visibleTimer = 0f;
        float displayedProgress = 0f;

        while (!loadOperation.isDone)
        {
            visibleTimer += Time.unscaledDeltaTime;

            float targetProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                progressSmoothingSpeed * Time.unscaledDeltaTime);

            UpdateProgressUI(displayedProgress);

            bool sceneReady = loadOperation.progress >= 0.9f;
            bool minTimeReached = visibleTimer >= minimumVisibleLoadingTime;

            if (sceneReady && minTimeReached)
                break;

            yield return null;
        }

        UpdateProgressUI(1f);

        // Уходим в темноту перед активацией новой сцены.
        yield return FadeBlackOverlay(0f, 1f, fadeToBlackDuration);

        // Прячем контент loading screen, оставляя только чёрный экран.
        loadingContentGroup.alpha = 0f;

        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
            yield return null;

        if (holdBlackAfterSceneActivation > 0f)
            yield return new WaitForSecondsRealtime(holdBlackAfterSceneActivation);

        // Дополнительный кадр, чтобы SceneBootstrap успел поставить игрока.
        yield return null;

        // Открываем уже новую сцену из темноты.
        yield return FadeBlackOverlay(1f, 0f, fadeIntoSceneDuration);

        HideScreenInstant();

        if (showLogs)
            Debug.Log($"[SceneTransitionManager] Scene loaded: {sceneName}", this);

        isLoading = false;
    }

    private void ShowScreenInstant()
    {
        if (screenRootGroup == null)
            return;

        screenRootGroup.gameObject.SetActive(true);
        screenRootGroup.alpha = 1f;
        screenRootGroup.blocksRaycasts = true;
        screenRootGroup.interactable = true;
    }

    private void HideScreenInstant()
    {
        if (loadingContentGroup != null)
            loadingContentGroup.alpha = 0f;

        SetBlackOverlayAlpha(0f);

        if (screenRootGroup != null)
        {
            screenRootGroup.alpha = 0f;
            screenRootGroup.blocksRaycasts = false;
            screenRootGroup.interactable = false;
            screenRootGroup.gameObject.SetActive(false);
        }

        ResetProgressUI();

        if (tipText != null)
            tipText.text = string.Empty;
    }

    private void ApplyRandomBackground()
    {
        if (backgroundImage == null)
            return;

        if (randomBackgrounds == null || randomBackgrounds.Length == 0)
        {
            backgroundImage.enabled = false;
            backgroundImage.sprite = null;
            return;
        }

        Sprite chosen = randomBackgrounds[Random.Range(0, randomBackgrounds.Length)];
        backgroundImage.sprite = chosen;
        backgroundImage.enabled = chosen != null;
    }

    private IEnumerator ApplyRandomTipRoutine()
    {
        if (tipText == null)
            yield break;

        tipText.text = string.Empty;

        if (randomTips == null || randomTips.Length == 0)
            yield break;

        LocalizedString chosen = randomTips[Random.Range(0, randomTips.Length)];
        if (chosen == null)
            yield break;

        AsyncOperationHandle<string> handle = chosen.GetLocalizedStringAsync();
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            tipText.text = handle.Result;
        else
            tipText.text = string.Empty;
    }

    private void ResetProgressUI()
    {
        UpdateProgressUI(0f);
    }

    private void UpdateProgressUI(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);

        if (progressBar != null)
            progressBar.value = normalizedValue;

        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(normalizedValue * 100f)}%";
    }

    private IEnumerator FadeBlackOverlay(float from, float to, float duration)
    {
        if (blackOverlay == null)
            yield break;

        if (duration <= 0f)
        {
            SetBlackOverlayAlpha(to);
            yield break;
        }

        float time = 0f;
        SetBlackOverlayAlpha(from);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            SetBlackOverlayAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetBlackOverlayAlpha(to);
    }

    private void SetBlackOverlayAlpha(float alpha)
    {
        if (blackOverlay == null)
            return;

        Color c = blackOverlay.color;
        c.a = Mathf.Clamp01(alpha);
        blackOverlay.color = c;
    }
}