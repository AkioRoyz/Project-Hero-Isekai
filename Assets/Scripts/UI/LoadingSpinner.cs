using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField] private bool clockwise = true;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float direction = clockwise ? -1f : 1f;
        rectTransform.Rotate(0f, 0f, degreesPerSecond * direction * Time.unscaledDeltaTime);
    }
}