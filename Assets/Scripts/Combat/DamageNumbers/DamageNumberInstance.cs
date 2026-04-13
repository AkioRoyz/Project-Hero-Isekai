using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class DamageNumberInstance : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private MeshRenderer meshRenderer;

    private DamageNumberStyle runtimeStyle;
    private Vector3 startWorldPosition;
    private Vector3 baseLocalScale;
    private float horizontalDrift;
    private float elapsedTime;
    private bool isInitialized;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        baseLocalScale = transform.localScale;
    }

    private void ResolveReferences()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Initialize(
        int amount,
        DamageNumberStyle style,
        Vector3 worldPosition,
        float driftAmount,
        string overrideText = null)
    {
        ResolveReferences();

        runtimeStyle = style;
        runtimeStyle.Sanitize();

        if (baseLocalScale == Vector3.zero)
            baseLocalScale = Vector3.one;

        startWorldPosition = worldPosition;
        horizontalDrift = driftAmount;
        elapsedTime = 0f;
        isInitialized = true;

        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = baseLocalScale * runtimeStyle.startScaleMultiplier;

        if (textMesh != null)
        {
            textMesh.text = string.IsNullOrEmpty(overrideText)
                ? $"{runtimeStyle.prefix}{amount}{runtimeStyle.suffix}"
                : overrideText;

            textMesh.fontSize = runtimeStyle.fontSize;
            textMesh.color = runtimeStyle.color;
            textMesh.alignment = TextAlignmentOptions.Center;
        }

        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = runtimeStyle.sortingLayerName;
            meshRenderer.sortingOrder = runtimeStyle.orderInLayer;
        }

        gameObject.name = $"DamageNumber_{runtimeStyle.type}";
    }

    private void Update()
    {
        if (!isInitialized || runtimeStyle == null)
            return;

        elapsedTime += Time.deltaTime;

        float lifetime = Mathf.Max(0.1f, runtimeStyle.lifetime);
        float normalizedTime = Mathf.Clamp01(elapsedTime / lifetime);

        UpdatePosition(normalizedTime);
        UpdateScale(normalizedTime);
        UpdateAlpha(normalizedTime);

        if (elapsedTime >= lifetime)
            Destroy(gameObject);
    }

    private void UpdatePosition(float normalizedTime)
    {
        Vector3 worldPosition = startWorldPosition;
        worldPosition.x += horizontalDrift * normalizedTime;
        worldPosition.y += runtimeStyle.riseDistance * normalizedTime;
        transform.position = worldPosition;
    }

    private void UpdateScale(float normalizedTime)
    {
        float scaleMultiplier;

        if (normalizedTime <= 0.20f)
        {
            float enterT = normalizedTime / 0.20f;
            scaleMultiplier = Mathf.Lerp(
                runtimeStyle.startScaleMultiplier,
                runtimeStyle.peakScaleMultiplier,
                enterT
            );
        }
        else
        {
            float settleT = (normalizedTime - 0.20f) / 0.80f;
            scaleMultiplier = Mathf.Lerp(
                runtimeStyle.peakScaleMultiplier,
                runtimeStyle.endScaleMultiplier,
                settleT
            );
        }

        transform.localScale = baseLocalScale * scaleMultiplier;
    }

    private void UpdateAlpha(float normalizedTime)
    {
        if (textMesh == null)
            return;

        Color color = runtimeStyle.color;

        if (normalizedTime >= runtimeStyle.fadeStartNormalized)
        {
            float fadeT = (normalizedTime - runtimeStyle.fadeStartNormalized) /
                          Mathf.Max(0.0001f, 1f - runtimeStyle.fadeStartNormalized);

            color.a = Mathf.Lerp(runtimeStyle.color.a, 0f, fadeT);
        }

        textMesh.color = color;
    }
}