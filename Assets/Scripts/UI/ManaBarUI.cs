using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [Header("Mana References")]
    [SerializeField] private Image manaBar;
    [SerializeField] private Image manaBarBackground;
    [SerializeField] private PlayerMana playerMana;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Fill Animation")]
    [SerializeField, Min(0.01f)] private float mainBarSharpness = 18f;
    [SerializeField, Min(0f)] private float backgroundDelay = 1f;
    [SerializeField, Min(0.01f)] private float backgroundSharpness = 5f;
    [SerializeField] private bool instantIncrease = false;

    private float targetFill = 1f;
    private float backgroundDelayTimer;
    private bool fillInitialized;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Rebind();
        UpdateUI();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Unbind();
    }

    private void Update()
    {
        UpdateBarAnimation(Time.unscaledDeltaTime);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebind();
        UpdateUI();
    }

    private void Rebind()
    {
        Unbind();
        ResolveReferences();

        fillInitialized = false;

        if (playerMana != null)
            playerMana.OnManaChange += UpdateUI;
    }

    private void Unbind()
    {
        if (playerMana != null)
            playerMana.OnManaChange -= UpdateUI;
    }

    private void ResolveReferences()
    {
        if (manaBar == null)
            manaBar = GetComponent<Image>();

        if (manaText == null)
            manaText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (playerMana == null)
            playerMana = FindFirstObjectByType<PlayerMana>();
    }

    private void UpdateUI()
    {
        if (playerMana == null)
            return;

        if (playerMana.MaxMana <= 0)
            return;

        int currentMana = playerMana.CurrentMana;
        int maxMana = playerMana.MaxMana;
        float newTargetFill = Mathf.Clamp01((float)currentMana / maxMana);

        bool fillChanged = !Mathf.Approximately(targetFill, newTargetFill);
        targetFill = newTargetFill;

        if (!fillInitialized)
        {
            fillInitialized = true;

            if (manaBar != null)
                manaBar.fillAmount = targetFill;

            if (manaBarBackground != null)
                manaBarBackground.fillAmount = targetFill;
        }
        else if (fillChanged)
        {
            backgroundDelayTimer = backgroundDelay;
        }

        if (manaText != null)
            manaText.text = $"{currentMana}/{maxMana}";
    }

    private void UpdateBarAnimation(float deltaTime)
    {
        if (!fillInitialized)
            return;

        if (manaBar != null)
        {
            manaBar.fillAmount = UpdateFillValue(
                manaBar.fillAmount,
                targetFill,
                mainBarSharpness,
                deltaTime,
                instantIncrease);
        }

        if (manaBarBackground != null)
        {
            if (backgroundDelayTimer > 0f)
            {
                backgroundDelayTimer -= deltaTime;
            }
            else
            {
                manaBarBackground.fillAmount = UpdateFillValue(
                    manaBarBackground.fillAmount,
                    targetFill,
                    backgroundSharpness,
                    deltaTime,
                    instantIncrease);
            }
        }
    }

    private static float UpdateFillValue(float current, float target, float sharpness, float deltaTime, bool instantIncrease)
    {
        current = Mathf.Clamp01(current);
        target = Mathf.Clamp01(target);

        if (instantIncrease && target > current)
            return target;

        if (Mathf.Approximately(current, target))
            return target;

        float t = 1f - Mathf.Exp(-sharpness * deltaTime);
        float value = Mathf.Lerp(current, target, t);

        if (Mathf.Abs(value - target) <= 0.001f)
            value = target;

        return Mathf.Clamp01(value);
    }
}
