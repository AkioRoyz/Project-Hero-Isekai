using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

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

        if (playerHealth != null)
            playerHealth.OnHealthChange += UpdateUI;
    }

    private void Unbind()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChange -= UpdateUI;
    }

    private void ResolveReferences()
    {
        if (healthBar == null)
            healthBar = GetComponent<Image>();

        if (healthText == null)
            healthText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void UpdateUI()
    {
        if (healthBar == null || healthText == null || playerHealth == null)
            return;

        if (playerHealth.MaxHealth <= 0)
            return;

        float fill = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth;
        healthBar.fillAmount = Mathf.Clamp01(fill);
        healthText.text = $"{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";
    }
}