using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TargetMarkerView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image image;
    [SerializeField] private Canvas canvas;

    [Header("Visual")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Color markerColor = Color.white;

    [Header("UI Image Settings")]
    [SerializeField] private Vector2 uiImageSize = new Vector2(64f, 64f);
    [SerializeField] private bool preserveImageAspect = true;
    [SerializeField] private bool imageRaycastTarget = false;

    [Header("World Settings")]
    [SerializeField][Min(0.01f)] private float worldScale = 1f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 220;
    [SerializeField] private bool applySortingToWorldCanvas = true;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private float baseZRotation = 0f;

    [Header("Bob Animation")]
    [SerializeField][Min(0f)] private float bobAmplitude = 0f;
    [SerializeField][Min(0f)] private float bobFrequency = 6f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Quarter Turn Effect")]
    [SerializeField] private bool enableQuarterTurnEffect = true;
    [SerializeField][Min(0.01f)] private float secondsBetweenTurns = 1.25f;
    [SerializeField][Min(0.01f)] private float turnDuration = 0.18f;
    [SerializeField] private float turnAngle = 90f;
    [SerializeField] private bool resetTurnTimerWhenShown = true;
    [SerializeField] private bool resetTurnRotationWhenHidden = true;

    [Header("Visibility")]
    [SerializeField] private bool setGameObjectActiveWhenVisible = true;

    private float timeSeed;
    private bool isVisible;

    private float currentTurnZ;
    private float turnStartZ;
    private float turnTargetZ;
    private float turnTimer;
    private float turnIntervalTimer;
    private bool isTurning;

    private void Reset()
    {
        ResolveReferences();
        ApplyVisuals();
        ApplyRotation();
    }

    private void Awake()
    {
        ResolveReferences();
        ApplyVisuals();

        timeSeed = Random.Range(0f, 100f);

        if (!isVisible)
            SetRenderersEnabled(false);

        ApplyRotation();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyVisuals();
        SetRenderersEnabled(isVisible);
        UpdatePositionIfPossible();
        ApplyRotation();
    }

    private void OnDisable()
    {
        if (resetTurnRotationWhenHidden)
            ResetQuarterTurnState(true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
        ApplyVisuals();
        ApplyRotation();
    }
#endif

    private void LateUpdate()
    {
        if (!isVisible)
            return;

        UpdatePositionIfPossible();
        UpdateQuarterTurnEffect();
        ApplyRotation();
    }

    public void SetTarget(Transform target)
    {
        SetTarget(target, true);
    }

    public void SetTarget(Transform target, bool snapImmediately)
    {
        followTarget = target;

        if (snapImmediately)
            UpdatePositionIfPossible();
    }

    public void ClearTarget()
    {
        followTarget = null;
    }

    public void SetVisible(bool value)
    {
        bool wasVisible = isVisible;
        isVisible = value;

        if (!value && resetTurnRotationWhenHidden)
            ResetQuarterTurnState(true);

        if (setGameObjectActiveWhenVisible)
        {
            if (gameObject.activeSelf != value)
                gameObject.SetActive(value);
        }

        ResolveReferences();
        SetRenderersEnabled(value);

        if (value && !wasVisible && resetTurnTimerWhenShown)
            ResetQuarterTurnState(false);

        if (value)
        {
            UpdatePositionIfPossible();
            ApplyRotation();
        }
    }

    public void SetSprite(Sprite sprite)
    {
        markerSprite = sprite;
        ApplyVisuals();
    }

    public void SetColor(Color color)
    {
        markerColor = color;
        ApplyVisuals();
    }

    public void SetWorldOffset(Vector3 offset)
    {
        worldOffset = offset;
        UpdatePositionIfPossible();
    }

    private void UpdatePositionIfPossible()
    {
        if (followTarget == null)
            return;

        float timeValue = useUnscaledTime ? Time.unscaledTime : Time.time;
        float bobOffset = bobAmplitude > 0f
            ? Mathf.Sin((timeValue + timeSeed) * bobFrequency) * bobAmplitude
            : 0f;

        Vector3 targetPosition = followTarget.position + worldOffset;
        targetPosition.y += bobOffset;

        transform.position = targetPosition;
    }

    private void UpdateQuarterTurnEffect()
    {
        if (!enableQuarterTurnEffect)
            return;

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (isTurning)
        {
            turnTimer += deltaTime;

            float t = Mathf.Clamp01(turnTimer / turnDuration);

            // Плавное начало и резкий конец.
            float easedT = EaseInCubic(t);

            currentTurnZ = Mathf.LerpUnclamped(turnStartZ, turnTargetZ, easedT);

            if (t >= 1f)
            {
                currentTurnZ = turnTargetZ;
                isTurning = false;
                turnTimer = 0f;
                turnIntervalTimer = 0f;
            }

            return;
        }

        turnIntervalTimer += deltaTime;

        if (turnIntervalTimer >= secondsBetweenTurns)
            StartQuarterTurn();
    }

    private void StartQuarterTurn()
    {
        isTurning = true;
        turnTimer = 0f;

        turnStartZ = currentTurnZ;
        turnTargetZ = currentTurnZ + turnAngle;
    }

    private void ResetQuarterTurnState(bool resetRotation)
    {
        isTurning = false;
        turnTimer = 0f;
        turnIntervalTimer = 0f;
        turnStartZ = 0f;
        turnTargetZ = 0f;

        if (resetRotation)
            currentTurnZ = 0f;

        ApplyRotation();
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, baseZRotation + currentTurnZ);
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (image == null)
            image = GetComponent<Image>();

        if (image == null)
            image = GetComponentInChildren<Image>(true);

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);
    }

    private void ApplyVisuals()
    {
        ResolveReferences();

        if (spriteRenderer != null)
        {
            if (markerSprite != null)
                spriteRenderer.sprite = markerSprite;

            spriteRenderer.color = markerColor;
            spriteRenderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName)
                ? "Default"
                : sortingLayerName;
            spriteRenderer.sortingOrder = orderInLayer;
        }

        if (image != null)
        {
            if (markerSprite != null)
                image.sprite = markerSprite;

            image.color = markerColor;
            image.preserveAspect = preserveImageAspect;
            image.raycastTarget = imageRaycastTarget;

            RectTransform imageRect = image.rectTransform;
            if (imageRect != null)
                imageRect.sizeDelta = uiImageSize;
        }

        if (canvas != null && applySortingToWorldCanvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.overrideSorting = true;
            canvas.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName)
                ? "Default"
                : sortingLayerName;
            canvas.sortingOrder = orderInLayer;
        }

        transform.localScale = Vector3.one * worldScale;
    }

    private void SetRenderersEnabled(bool value)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = value;

        if (image != null)
            image.enabled = value;
    }
}