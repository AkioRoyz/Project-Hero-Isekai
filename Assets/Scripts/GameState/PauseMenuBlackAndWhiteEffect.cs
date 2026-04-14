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

    private void Awake()
    {
        DisablePauseEffectImmediate();
    }

    public void EnablePauseEffect()
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

    public void DisablePauseEffect()
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

    public void DisablePauseEffectImmediate()
    {
        DisablePauseEffect();
    }
}