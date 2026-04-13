using TMPro;
using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private DamageNumberInstance damageNumberPrefab;

    [Header("World Container")]
    [SerializeField] private Transform worldContainer;
    [SerializeField] private bool createContainerIfMissing = true;

    [Header("Styles")]
    [SerializeField] private DamageNumberStyle[] styles;

    private bool missingPrefabWarningShown;

    private void Reset()
    {
        EnsureDefaultStyles();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureDefaultStylesIfNeeded();
        SanitizeStyles();
        EnsureWorldContainer();
    }

    private void OnValidate()
    {
        EnsureDefaultStylesIfNeeded();
        SanitizeStyles();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool ShowDamageNumber(CombatTarget target, int amount, DamageNumberType type, string overrideText = null)
    {
        if (amount <= 0)
            return false;

        DamageNumberStyle style = GetStyle(type);
        if (style == null)
            return false;

        Vector3 spawnPosition = ResolveSpawnPosition(target, style);
        float horizontalDrift = style.GetRandomHorizontalDrift();

        DamageNumberInstance instance = CreateInstance();
        if (instance == null)
            return false;

        instance.Initialize(amount, style, spawnPosition, horizontalDrift, overrideText);
        return true;
    }

    public bool ShowDamageNumber(GameObject targetObject, int amount, DamageNumberType type, string overrideText = null)
    {
        if (targetObject == null)
            return ShowDamageNumber(Vector3.zero, amount, type, overrideText);

        CombatTarget combatTarget = targetObject.GetComponentInParent<CombatTarget>();
        if (combatTarget == null)
            combatTarget = targetObject.GetComponentInChildren<CombatTarget>();

        if (combatTarget != null)
            return ShowDamageNumber(combatTarget, amount, type, overrideText);

        return ShowDamageNumber(targetObject.transform.position, amount, type, overrideText);
    }

    public bool ShowDamageNumber(Vector3 worldPosition, int amount, DamageNumberType type, string overrideText = null)
    {
        if (amount <= 0)
            return false;

        DamageNumberStyle style = GetStyle(type);
        if (style == null)
            return false;

        Vector3 spawnPosition = worldPosition;
        spawnPosition.x += style.GetRandomSpawnOffsetX();
        spawnPosition.y += style.GetRandomSpawnOffsetY();

        float horizontalDrift = style.GetRandomHorizontalDrift();

        DamageNumberInstance instance = CreateInstance();
        if (instance == null)
            return false;

        instance.Initialize(amount, style, spawnPosition, horizontalDrift, overrideText);
        return true;
    }

    private Vector3 ResolveSpawnPosition(CombatTarget target, DamageNumberStyle style)
    {
        Vector3 spawnPosition;

        if (target != null && target.AimPoint != null)
            spawnPosition = target.AimPoint.position;
        else if (target != null)
            spawnPosition = target.transform.position;
        else
            spawnPosition = Vector3.zero;

        spawnPosition.x += style.GetRandomSpawnOffsetX();
        spawnPosition.y += style.GetRandomSpawnOffsetY();

        return spawnPosition;
    }

    private DamageNumberInstance CreateInstance()
    {
        EnsureWorldContainer();

        if (damageNumberPrefab != null)
        {
            Transform parent = worldContainer != null ? worldContainer : transform;
            return Instantiate(damageNumberPrefab, parent);
        }

        if (!missingPrefabWarningShown)
        {
            Debug.LogWarning(
                "DamageNumberManager: Damage Number Prefab is not assigned. " +
                "A temporary fallback TextMeshPro object will be created at runtime.",
                this
            );

            missingPrefabWarningShown = true;
        }

        return CreateFallbackInstance();
    }

    private DamageNumberInstance CreateFallbackInstance()
    {
        Transform parent = worldContainer != null ? worldContainer : transform;

        GameObject go = new GameObject("DamageNumber_Fallback");
        go.transform.SetParent(parent, false);

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = "0";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 3f;

        DamageNumberInstance instance = go.AddComponent<DamageNumberInstance>();
        return instance;
    }

    private void EnsureWorldContainer()
    {
        if (worldContainer != null)
            return;

        if (!createContainerIfMissing)
            return;

        GameObject containerObject = new GameObject("DamageNumbers_WorldContainer");
        containerObject.transform.SetParent(transform, false);
        worldContainer = containerObject.transform;
    }

    private DamageNumberStyle GetStyle(DamageNumberType type)
    {
        if (styles == null || styles.Length == 0)
            return null;

        for (int i = 0; i < styles.Length; i++)
        {
            if (styles[i] != null && styles[i].type == type)
                return styles[i];
        }

        for (int i = 0; i < styles.Length; i++)
        {
            if (styles[i] != null && styles[i].type == DamageNumberType.Normal)
                return styles[i];
        }

        for (int i = 0; i < styles.Length; i++)
        {
            if (styles[i] != null)
                return styles[i];
        }

        return null;
    }

    private void SanitizeStyles()
    {
        if (styles == null)
            return;

        for (int i = 0; i < styles.Length; i++)
        {
            if (styles[i] != null)
                styles[i].Sanitize();
        }
    }

    private void EnsureDefaultStylesIfNeeded()
    {
        if (styles != null && styles.Length > 0)
            return;

        EnsureDefaultStyles();
    }

    private void EnsureDefaultStyles()
    {
        styles = new DamageNumberStyle[5];

        styles[0] = CreateDefaultStyle(
            DamageNumberType.Normal,
            new Color(1.00f, 0.95f, 0.85f, 1f),
            3.2f,
            0.70f,
            0.75f,
            "Default",
            120
        );

        styles[1] = CreateDefaultStyle(
            DamageNumberType.Critical,
            new Color(1.00f, 0.55f, 0.15f, 1f),
            3.8f,
            0.85f,
            0.95f,
            "Default",
            125
        );

        styles[2] = CreateDefaultStyle(
            DamageNumberType.Special,
            new Color(0.35f, 1.00f, 0.95f, 1f),
            4.0f,
            0.90f,
            1.00f,
            "Default",
            130
        );

        styles[3] = CreateDefaultStyle(
            DamageNumberType.Blocked,
            new Color(0.80f, 0.80f, 0.80f, 1f),
            3.0f,
            0.70f,
            0.65f,
            "Default",
            118
        );

        styles[4] = CreateDefaultStyle(
            DamageNumberType.ParryCounter,
            new Color(0.65f, 1.00f, 0.65f, 1f),
            3.6f,
            0.80f,
            0.90f,
            "Default",
            128
        );
    }

    private DamageNumberStyle CreateDefaultStyle(
        DamageNumberType type,
        Color color,
        float fontSize,
        float lifetime,
        float riseDistance,
        string sortingLayerName,
        int orderInLayer)
    {
        DamageNumberStyle style = new DamageNumberStyle();
        style.type = type;
        style.color = color;
        style.fontSize = fontSize;
        style.lifetime = lifetime;
        style.riseDistance = riseDistance;
        style.fadeStartNormalized = 0.55f;
        style.randomSpawnOffsetX = new Vector2(-0.15f, 0.15f);
        style.randomSpawnOffsetY = new Vector2(0.00f, 0.18f);
        style.horizontalDriftRange = new Vector2(-0.08f, 0.08f);
        style.startScaleMultiplier = 1.00f;
        style.peakScaleMultiplier = 1.15f;
        style.endScaleMultiplier = 0.90f;
        style.sortingLayerName = sortingLayerName;
        style.orderInLayer = orderInLayer;
        style.prefix = "";
        style.suffix = "";
        style.Sanitize();
        return style;
    }
}