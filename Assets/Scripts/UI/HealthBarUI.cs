using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health References")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Fill Animation")]
    [SerializeField, Min(0.01f)] private float mainBarSharpness = 18f;
    [SerializeField, Min(0f)] private float backgroundDelay = 1f;
    [SerializeField, Min(0.01f)] private float backgroundSharpness = 5f;
    [SerializeField] private bool instantIncrease = false;

    [Header("Avatar Reference")]
    [SerializeField] private Image avatarImage;

    [Header("Avatar Sprites")]
    [Tooltip("Sprite 1. Temporarily used right after taking damage.")]
    [SerializeField] private Sprite avatarDamageSprite;

    [Tooltip("Sprite 2. Used when health is 0.")]
    [SerializeField] private Sprite avatarDeadSprite;

    [Tooltip("Sprite 3. Used when health is 1-10%.")]
    [SerializeField] private Sprite avatarHealth01To10Sprite;

    [Tooltip("Sprite 4. Used when health is 11-40%.")]
    [SerializeField] private Sprite avatarHealth11To40Sprite;

    [Tooltip("Sprite 5. Used when health is 41-60%.")]
    [SerializeField] private Sprite avatarHealth41To60Sprite;

    [Tooltip("Sprite 6. Used when health is 61-80%.")]
    [SerializeField] private Sprite avatarHealth61To80Sprite;

    [Tooltip("Sprite 7. Used when health is 81-100%.")]
    [SerializeField] private Sprite avatarHealth81To100Sprite;

    [Header("Avatar Damage Feedback")]
    [SerializeField, Min(0f)] private float damageSpriteDuration = 0.25f;
    [SerializeField, Min(0f)] private float avatarShakeDuration = 0.25f;
    [SerializeField, Min(0f)] private float avatarShakeStrength = 6f;

    private float targetFill = 1f;
    private float backgroundDelayTimer;
    private bool fillInitialized;

    private int lastHealth = -1;

    private RectTransform avatarRectTransform;
    private Vector2 avatarDefaultAnchoredPosition;
    private float damageSpriteTimer;
    private float avatarShakeTimer;

    private void Awake()
    {
        ResolveReferences();
        CacheAvatarPosition();
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
        ResetAvatarPosition();
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        UpdateBarAnimation(deltaTime);
        UpdateAvatarFeedback(deltaTime);
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
        CacheAvatarPosition();

        fillInitialized = false;
        lastHealth = -1;
        damageSpriteTimer = 0f;
        avatarShakeTimer = 0f;

        if (playerHealth != null)
            playerHealth.OnHealthChange += UpdateUI;
    }

    private void Unbind()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChange -= UpdateUI;
    }

    private void ResolveReferences()
    {
        if (healthBar == null)
            healthBar = GetComponent<Image>();

        if (healthText == null)
            healthText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (avatarImage != null && avatarRectTransform == null)
            avatarRectTransform = avatarImage.rectTransform;
    }

    private void CacheAvatarPosition()
    {
        if (avatarImage == null)
            return;

        avatarRectTransform = avatarImage.rectTransform;
        avatarDefaultAnchoredPosition = avatarRectTransform.anchoredPosition;
    }

    private void UpdateUI()
    {
        if (playerHealth == null)
            return;

        if (playerHealth.MaxHealth <= 0)
            return;

        int currentHealth = playerHealth.CurrentHealth;
        int maxHealth = playerHealth.MaxHealth;
        float newTargetFill = Mathf.Clamp01((float)currentHealth / maxHealth);

        bool tookDamage = lastHealth >= 0 && currentHealth < lastHealth;
        bool fillChanged = !Mathf.Approximately(targetFill, newTargetFill);

        targetFill = newTargetFill;

        if (!fillInitialized)
        {
            fillInitialized = true;

            if (healthBar != null)
                healthBar.fillAmount = targetFill;

            if (healthBarBackground != null)
                healthBarBackground.fillAmount = targetFill;
        }
        else if (fillChanged)
        {
            backgroundDelayTimer = backgroundDelay;
        }

        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";

        if (tookDamage)
            PlayAvatarDamageFeedback();

        lastHealth = currentHealth;
        UpdateAvatarSprite();
    }

    private void UpdateBarAnimation(float deltaTime)
    {
        if (!fillInitialized)
            return;

        if (healthBar != null)
        {
            healthBar.fillAmount = UpdateFillValue(
                healthBar.fillAmount,
                targetFill,
                mainBarSharpness,
                deltaTime,
                instantIncrease);
        }

        if (healthBarBackground != null)
        {
            if (backgroundDelayTimer > 0f)
            {
                backgroundDelayTimer -= deltaTime;
            }
            else
            {
                healthBarBackground.fillAmount = UpdateFillValue(
                    healthBarBackground.fillAmount,
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

    private void PlayAvatarDamageFeedback()
    {
        damageSpriteTimer = damageSpriteDuration;
        avatarShakeTimer = avatarShakeDuration;
        UpdateAvatarSprite();
    }

    private void UpdateAvatarFeedback(float deltaTime)
    {
        if (damageSpriteTimer > 0f)
        {
            damageSpriteTimer -= deltaTime;

            if (damageSpriteTimer <= 0f)
            {
                damageSpriteTimer = 0f;
                UpdateAvatarSprite();
            }
        }

        if (avatarRectTransform == null)
            return;

        if (avatarShakeTimer > 0f)
        {
            avatarShakeTimer -= deltaTime;

            float normalizedTime = avatarShakeDuration <= 0f ? 0f : avatarShakeTimer / avatarShakeDuration;
            float strength = avatarShakeStrength * Mathf.Clamp01(normalizedTime);
            Vector2 shakeOffset = Random.insideUnitCircle * strength;
            avatarRectTransform.anchoredPosition = avatarDefaultAnchoredPosition + shakeOffset;
        }
        else
        {
            ResetAvatarPosition();
        }
    }

    private void UpdateAvatarSprite()
    {
        if (avatarImage == null || playerHealth == null || playerHealth.MaxHealth <= 0)
            return;

        Sprite targetSprite = null;

        if (damageSpriteTimer > 0f && avatarDamageSprite != null)
        {
            targetSprite = avatarDamageSprite;
        }
        else
        {
            int currentHealth = playerHealth.CurrentHealth;
            float healthPercent = Mathf.Clamp01((float)currentHealth / playerHealth.MaxHealth) * 100f;

            if (currentHealth <= 0)
                targetSprite = avatarDeadSprite;
            else if (healthPercent <= 10f)
                targetSprite = avatarHealth01To10Sprite;
            else if (healthPercent <= 40f)
                targetSprite = avatarHealth11To40Sprite;
            else if (healthPercent <= 60f)
                targetSprite = avatarHealth41To60Sprite;
            else if (healthPercent <= 80f)
                targetSprite = avatarHealth61To80Sprite;
            else
                targetSprite = avatarHealth81To100Sprite;
        }

        if (targetSprite != null)
            avatarImage.sprite = targetSprite;
    }

    private void ResetAvatarPosition()
    {
        if (avatarRectTransform == null)
            return;

        avatarRectTransform.anchoredPosition = avatarDefaultAnchoredPosition;
    }
}
