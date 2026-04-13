using UnityEngine;

[System.Serializable]
public class DamageNumberStyle
{
    public DamageNumberType type = DamageNumberType.Normal;

    [Header("Text")]
    public Color color = Color.white;
    [Min(0.1f)] public float fontSize = 3f;
    public string prefix = "";
    public string suffix = "";

    [Header("Lifetime")]
    [Min(0.1f)] public float lifetime = 0.7f;
    [Min(0f)] public float riseDistance = 0.75f;
    [Range(0f, 0.98f)] public float fadeStartNormalized = 0.55f;

    [Header("Spawn Offset")]
    public Vector2 randomSpawnOffsetX = new Vector2(-0.15f, 0.15f);
    public Vector2 randomSpawnOffsetY = new Vector2(0.00f, 0.18f);

    [Header("Horizontal Drift")]
    public Vector2 horizontalDriftRange = new Vector2(-0.08f, 0.08f);

    [Header("Scale Animation")]
    [Min(0.01f)] public float startScaleMultiplier = 1.00f;
    [Min(0.01f)] public float peakScaleMultiplier = 1.15f;
    [Min(0.01f)] public float endScaleMultiplier = 0.90f;

    [Header("Sorting")]
    public string sortingLayerName = "Default";
    public int orderInLayer = 100;

    public float GetRandomSpawnOffsetX()
    {
        return Random.Range(
            Mathf.Min(randomSpawnOffsetX.x, randomSpawnOffsetX.y),
            Mathf.Max(randomSpawnOffsetX.x, randomSpawnOffsetX.y)
        );
    }

    public float GetRandomSpawnOffsetY()
    {
        return Random.Range(
            Mathf.Min(randomSpawnOffsetY.x, randomSpawnOffsetY.y),
            Mathf.Max(randomSpawnOffsetY.x, randomSpawnOffsetY.y)
        );
    }

    public float GetRandomHorizontalDrift()
    {
        return Random.Range(
            Mathf.Min(horizontalDriftRange.x, horizontalDriftRange.y),
            Mathf.Max(horizontalDriftRange.x, horizontalDriftRange.y)
        );
    }

    public void Sanitize()
    {
        fontSize = Mathf.Max(0.1f, fontSize);
        lifetime = Mathf.Max(0.1f, lifetime);
        riseDistance = Mathf.Max(0f, riseDistance);
        fadeStartNormalized = Mathf.Clamp(fadeStartNormalized, 0f, 0.98f);

        startScaleMultiplier = Mathf.Max(0.01f, startScaleMultiplier);
        peakScaleMultiplier = Mathf.Max(0.01f, peakScaleMultiplier);
        endScaleMultiplier = Mathf.Max(0.01f, endScaleMultiplier);

        if (prefix == null)
            prefix = string.Empty;

        if (suffix == null)
            suffix = string.Empty;

        if (string.IsNullOrWhiteSpace(sortingLayerName))
            sortingLayerName = "Default";
    }
}