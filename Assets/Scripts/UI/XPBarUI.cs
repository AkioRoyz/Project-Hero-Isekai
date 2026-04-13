using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class XPBarUI : MonoBehaviour
{
    [SerializeField] private Image xpBar;
    [SerializeField] private ExpSystem expSystem;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI lvlText;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Rebind();
        ForceRefresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Unbind();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebind();
        ForceRefresh();
    }

    private void Rebind()
    {
        Unbind();
        ResolveReferences();

        if (expSystem != null)
            expSystem.OnXpAdd += OnXpChanged;
    }

    private void Unbind()
    {
        if (expSystem != null)
            expSystem.OnXpAdd -= OnXpChanged;
    }

    private void ResolveReferences()
    {
        if (xpBar == null)
            xpBar = GetComponent<Image>();

        if (expText == null || lvlText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (expText == null && texts.Length > 0) expText = texts[0];
            if (lvlText == null && texts.Length > 1) lvlText = texts[1];
        }

        if (expSystem == null)
            expSystem = FindFirstObjectByType<ExpSystem>();
    }

    private void ForceRefresh()
    {
        if (expSystem == null)
        {
            if (xpBar != null) xpBar.fillAmount = 0f;
            if (expText != null) expText.text = "0/0";
            if (lvlText != null) lvlText.text = "-";
            return;
        }

        OnXpChanged(expSystem.CurrentXP);
    }

    private void OnXpChanged(int currentXP)
    {
        if (xpBar == null || expText == null || lvlText == null || expSystem == null)
            return;

        int xpToNextLvl = Mathf.Max(1, expSystem.XpToNextLvl);
        float fill = (float)currentXP / xpToNextLvl;

        xpBar.fillAmount = Mathf.Clamp01(fill);
        expText.text = $"{currentXP}/{xpToNextLvl}";
        lvlText.text = expSystem.CurrentLvl.ToString();
    }
}