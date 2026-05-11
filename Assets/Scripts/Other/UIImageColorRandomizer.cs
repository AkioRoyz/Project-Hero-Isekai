using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageColorRandomizer : MonoBehaviour
{
    [Header("Color Settings")]
    [Tooltip("Список цветов, между которыми будут происходить переходы")]
    public List<Color> colors = new List<Color>();

    [Tooltip("Время перехода между цветами")]
    public float transitionDuration = 2f;

    [Tooltip("Пауза между переходами")]
    public float delayBetweenTransitions = 0.5f;

    private Image targetImage;
    private Color currentColor;

    private void Awake()
    {
        targetImage = GetComponent<Image>();

        if (colors.Count > 0)
        {
            currentColor = colors[Random.Range(0, colors.Count)];
            targetImage.color = currentColor;
        }
    }

    private void OnEnable()
    {
        if (colors.Count > 1)
        {
            StartCoroutine(ColorTransitionLoop());
        }
    }

    private IEnumerator ColorTransitionLoop()
    {
        while (true)
        {
            Color nextColor = GetRandomColorDifferentFrom(currentColor);

            yield return StartCoroutine(LerpColor(currentColor, nextColor));

            currentColor = nextColor;

            if (delayBetweenTransitions > 0)
                yield return new WaitForSecondsRealtime(delayBetweenTransitions);
        }
    }

    private IEnumerator LerpColor(Color from, Color to)
    {
        float time = 0f;

        while (time < transitionDuration)
        {
            time += Time.unscaledDeltaTime; // 👈 главное изменение
            float t = time / transitionDuration;

            targetImage.color = Color.Lerp(from, to, t);

            yield return null;
        }

        targetImage.color = to;
    }

    private Color GetRandomColorDifferentFrom(Color current)
    {
        if (colors.Count <= 1)
            return current;

        Color newColor;
        int safety = 0;

        do
        {
            newColor = colors[Random.Range(0, colors.Count)];
            safety++;
        }
        while (newColor == current && safety < 10);

        return newColor;
    }
}