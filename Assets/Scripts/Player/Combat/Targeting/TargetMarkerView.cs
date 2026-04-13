using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class TargetMarkerView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Visual")]
    [SerializeField] private string markerText = "▼";
    [SerializeField] private Color markerColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField][Min(0.1f)] private float fontSize = 3.2f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 220;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.30f, 0f);
    [SerializeField][Min(0f)] private float bobAmplitude = 0.05f;
    [SerializeField][Min(0f)] private float bobFrequency = 6f;
    [SerializeField] private bool useUnscaledTime = true;

    private float timeSeed;

    private void Reset()
    {
        ResolveReferences();
        ApplyVisuals();
    }

    private void Awake()
    {
        ResolveReferences();
        ApplyVisuals();
        timeSeed = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        ApplyVisuals();
    }

    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        float timeValue = useUnscaledTime ? Time.unscaledTime : Time.time;

        Vector3 worldPosition = followTarget.position + worldOffset;
        worldPosition.y += Mathf.Sin((timeValue + timeSeed) * bobFrequency) * bobAmplitude;

        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;
    }

    public void SetTarget(Transform target)
    {
        followTarget = target;
    }

    public void SetVisible(bool value)
    {
        if (gameObject.activeSelf != value)
            gameObject.SetActive(value);
    }

    private void ResolveReferences()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    private void ApplyVisuals()
    {
        ResolveReferences();

        if (textMesh != null)
        {
            textMesh.text = string.IsNullOrWhiteSpace(markerText) ? "▼" : markerText;
            textMesh.fontSize = Mathf.Max(0.1f, fontSize);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = markerColor;
        }

        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName) ? "Default" : sortingLayerName;
            meshRenderer.sortingOrder = orderInLayer;
        }
    }
}