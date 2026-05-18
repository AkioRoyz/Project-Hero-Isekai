using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class XPBarUI : MonoBehaviour
{
    [Header("XP Bar")]
    [SerializeField] private Image xpBar;
    [SerializeField] private ExpSystem expSystem;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI lvlText;

    [Header("XP Fill Animation")]
    [SerializeField, Min(0f)] private float xpFillDuration = 0.45f;
    [SerializeField, Min(0f)] private float levelUpFillToFullDuration = 0.18f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Level Up Punch Effect")]
    [SerializeField] private GameObject levelUpPunchTarget;
    [SerializeField, Min(1f)] private float levelUpScaleMultiplier = 1.18f;
    [SerializeField, Min(0f)] private float levelUpScaleReturnDuration = 0.28f;

    private ExpSystem boundExpSystem;

    private Coroutine fillCoroutine;
    private Coroutine levelUpPunchCoroutine;

    private int lastKnownLevel = -1;
    private bool initialized;

    private Vector3 levelUpTargetBaseScale = Vector3.one;

    private void Awake()
    {
        ResolveReferences();
        CacheLevelUpTargetBaseScale();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        Rebind();
        ForceRefresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        StopFillAnimation();
        StopLevelUpPunchAnimation(true);

        Unbind();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebind();
        ForceRefresh();
    }

    private void Rebind()
    {
        Unbind();
        ResolveReferences();

        if (expSystem == null)
            return;

        boundExpSystem = expSystem;
        boundExpSystem.OnXpAdd += OnXpChanged;
    }

    private void Unbind()
    {
        if (boundExpSystem != null)
        {
            boundExpSystem.OnXpAdd -= OnXpChanged;
            boundExpSystem = null;
        }
    }

    private void ResolveReferences()
    {
        if (xpBar == null)
            xpBar = GetComponent<Image>();

        if (expText == null || lvlText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

            if (expText == null && texts.Length > 0)
                expText = texts[0];

            if (lvlText == null && texts.Length > 1)
                lvlText = texts[1];
        }

        if (expSystem == null)
            expSystem = FindFirstObjectByType<ExpSystem>();
    }

    private void CacheLevelUpTargetBaseScale()
    {
        if (levelUpPunchTarget != null)
            levelUpTargetBaseScale = levelUpPunchTarget.transform.localScale;
    }

    private void ForceRefresh()
    {
        StopFillAnimation();

        if (expSystem == null)
        {
            if (xpBar != null)
                xpBar.fillAmount = 0f;

            if (expText != null)
                expText.text = "0/0";

            if (lvlText != null)
                lvlText.text = "-";

            lastKnownLevel = -1;
            initialized = false;
            return;
        }

        int currentXP = expSystem.CurrentXP;
        int xpToNextLvl = Mathf.Max(1, expSystem.XpToNextLvl);
        int currentLevel = expSystem.CurrentLvl;

        float fill = Mathf.Clamp01((float)currentXP / xpToNextLvl);

        if (xpBar != null)
            xpBar.fillAmount = fill;

        UpdateTexts(currentXP, xpToNextLvl, currentLevel);

        lastKnownLevel = currentLevel;
        initialized = true;
    }

    private void OnXpChanged(int currentXP)
    {
        if (expSystem == null)
            return;

        int xpToNextLvl = Mathf.Max(1, expSystem.XpToNextLvl);
        int currentLevel = expSystem.CurrentLvl;

        float targetFill = Mathf.Clamp01((float)currentXP / xpToNextLvl);

        bool leveledUp = initialized && currentLevel > lastKnownLevel;

        UpdateTexts(currentXP, xpToNextLvl, currentLevel);

        if (xpBar != null)
        {
            StopFillAnimation();

            if (leveledUp)
            {
                fillCoroutine = StartCoroutine(LevelUpFillRoutine(targetFill));
            }
            else
            {
                fillCoroutine = StartCoroutine(AnimateFillRoutine(
                    xpBar.fillAmount,
                    targetFill,
                    xpFillDuration
                ));
            }
        }

        if (leveledUp)
            PlayLevelUpPunch();

        lastKnownLevel = currentLevel;
        initialized = true;
    }

    private void UpdateTexts(int currentXP, int xpToNextLvl, int currentLevel)
    {
        if (expText != null)
            expText.text = $"{currentXP}/{xpToNextLvl}";

        if (lvlText != null)
            lvlText.text = currentLevel.ToString();
    }

    private IEnumerator LevelUpFillRoutine(float targetFillAfterLevelUp)
    {
        float currentFill = xpBar.fillAmount;

        if (currentFill < 0.999f)
        {
            yield return AnimateFillRoutine(
                currentFill,
                1f,
                levelUpFillToFullDuration
            );
        }

        xpBar.fillAmount = 0f;

        if (targetFillAfterLevelUp > 0f)
        {
            yield return AnimateFillRoutine(
                0f,
                targetFillAfterLevelUp,
                xpFillDuration
            );
        }
    }

    private IEnumerator AnimateFillRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            xpBar.fillAmount = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();

            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            xpBar.fillAmount = Mathf.Lerp(from, to, easedT);

            yield return null;
        }

        xpBar.fillAmount = to;
    }

    private void PlayLevelUpPunch()
    {
        if (levelUpPunchTarget == null)
            return;

        StopLevelUpPunchAnimation(false);

        if (levelUpTargetBaseScale == Vector3.zero)
            CacheLevelUpTargetBaseScale();

        levelUpPunchCoroutine = StartCoroutine(LevelUpPunchRoutine());
    }

    private IEnumerator LevelUpPunchRoutine()
    {
        Transform target = levelUpPunchTarget.transform;

        Vector3 startScale = levelUpTargetBaseScale * levelUpScaleMultiplier;
        Vector3 endScale = levelUpTargetBaseScale;

        target.localScale = startScale;

        if (levelUpScaleReturnDuration <= 0f)
        {
            target.localScale = endScale;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < levelUpScaleReturnDuration)
        {
            elapsed += GetDeltaTime();

            float t = Mathf.Clamp01(elapsed / levelUpScaleReturnDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            target.localScale = Vector3.Lerp(startScale, endScale, easedT);

            yield return null;
        }

        target.localScale = endScale;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void StopFillAnimation()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
    }

    private void StopLevelUpPunchAnimation(bool resetScale)
    {
        if (levelUpPunchCoroutine != null)
        {
            StopCoroutine(levelUpPunchCoroutine);
            levelUpPunchCoroutine = null;
        }

        if (resetScale && levelUpPunchTarget != null)
            levelUpPunchTarget.transform.localScale = levelUpTargetBaseScale;
    }
}