using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [SerializeField] private Image manaBar;
    [SerializeField] private PlayerMana playerMana;
    [SerializeField] private TextMeshProUGUI manaText;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Rebind();
        UpdateUI();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Unbind();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebind();
        UpdateUI();
    }

    private void Rebind()
    {
        Unbind();
        ResolveReferences();

        if (playerMana != null)
            playerMana.OnManaChange += UpdateUI;
    }

    private void Unbind()
    {
        if (playerMana != null)
            playerMana.OnManaChange -= UpdateUI;
    }

    private void ResolveReferences()
    {
        if (manaBar == null)
            manaBar = GetComponent<Image>();

        if (manaText == null)
            manaText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (playerMana == null)
            playerMana = FindFirstObjectByType<PlayerMana>();
    }

    private void UpdateUI()
    {
        if (manaBar == null || manaText == null || playerMana == null)
            return;

        if (playerMana.MaxMana <= 0)
            return;

        float fill = (float)playerMana.CurrentMana / playerMana.MaxMana;
        manaBar.fillAmount = Mathf.Clamp01(fill);
        manaText.text = $"{playerMana.CurrentMana}/{playerMana.MaxMana}";
    }
}