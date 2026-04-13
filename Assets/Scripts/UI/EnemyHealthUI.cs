using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] Image enemyHealthBar;
    [SerializeField] EnemyHealth enemyHealth;

    private void Start()
    {
        UpdateUI();
    }

    private void OnEnable()
    {
        enemyHealth.OnEnemyHealthChange += UpdateUI;
    }

    private void OnDisable()
    {
        enemyHealth.OnEnemyHealthChange -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (enemyHealth.MaxHealth <= 0) return;

        float fill = (float)enemyHealth.CurrentHealth / enemyHealth.MaxHealth;
        fill = Mathf.Clamp01(fill);

        enemyHealthBar.fillAmount = fill;
    }
}
