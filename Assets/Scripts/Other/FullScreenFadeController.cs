using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class FullScreenFadeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fadeImage;
    [SerializeField] private TMP_Text messageText;

    private void Awake()
    {
        SetImmediateClear();
    }

    public void SetImmediateClear()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        SetRaycast(false);

        if (messageText != null)
            messageText.text = string.Empty;
    }

    public IEnumerator FadeOut(float duration)
    {
        if (canvasGroup == null)
            yield break;

        SetRaycast(true);
        duration = Mathf.Max(0.01f, duration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeIn(float duration)
    {
        if (canvasGroup == null)
            yield break;

        SetRaycast(true);
        duration = Mathf.Max(0.01f, duration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        SetImmediateClear();
    }

    public IEnumerator FadeOutWithMessage(LocalizedString message, float fadeDuration, float holdDuration)
    {
        if (messageText != null)
        {
            messageText.text = string.Empty;

            if (message != null)
            {
                var handle = message.GetLocalizedStringAsync();
                yield return handle;
                messageText.text = handle.Result;
            }
        }

        yield return FadeOut(fadeDuration);

        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);
    }

    private void SetRaycast(bool enabled)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = enabled;
            canvasGroup.interactable = enabled;
        }

        if (fadeImage != null)
            fadeImage.raycastTarget = enabled;
    }
}