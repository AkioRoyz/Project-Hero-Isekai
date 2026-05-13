using UnityEngine;
using UnityEngine.Rendering;

public class PauseMenuBlackAndWhiteEffect : MonoBehaviour
{
    [Header("Optional Root")]
    [SerializeField] private GameObject effectRoot;

    [Header("Optional Volume")]
    [SerializeField] private Volume targetVolume;
    [SerializeField] private float enabledWeight = 1f;
    [SerializeField] private float disabledWeight = 0f;

    [Header("Behaviour")]
    [SerializeField] private bool activateRootWhenEnabled = true;
    [SerializeField] private bool deactivateRootWhenDisabled = false;

    private int activeRequestCount;

    public bool IsEffectActive => activeRequestCount > 0;

    private void Awake()
    {
        DisablePauseEffectImmediate();
    }

    public void EnablePauseEffect()
    {
        activeRequestCount = Mathf.Max(0, activeRequestCount) + 1;
        ApplyEnabledState();
    }

    public void DisablePauseEffect()
    {
        if (activeRequestCount > 0)
        {
            activeRequestCount--;
        }

        if (activeRequestCount <= 0)
        {
            activeRequestCount = 0;
            ApplyDisabledState();
        }
    }

    public void DisablePauseEffectImmediate()
    {
        activeRequestCount = 0;
        ApplyDisabledState();
    }

    private void ApplyEnabledState()
    {
        if (activateRootWhenEnabled && effectRoot != null)
        {
            effectRoot.SetActive(true);
        }

        if (targetVolume != null)
        {
            targetVolume.weight = enabledWeight;
        }
    }

    private void ApplyDisabledState()
    {
        if (targetVolume != null)
        {
            targetVolume.weight = disabledWeight;
        }

        if (deactivateRootWhenDisabled && effectRoot != null)
        {
            effectRoot.SetActive(false);
        }
    }
}
