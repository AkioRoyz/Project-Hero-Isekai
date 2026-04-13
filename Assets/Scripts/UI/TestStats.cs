using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TestStats : MonoBehaviour
{
    private int currentHealth;
    private int maxHealth;
    private int currentMana;
    private int maxMana;
    private int currentLvl;
    private int maxLvl;
    private int strength;
    private int mana;
    private int defence;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMana playerMana;
    [SerializeField] private ExpSystem expSystem;
    [SerializeField] private StatsSystem statsSystem;

    [SerializeField] TextMeshProUGUI testHealthText;
    [SerializeField] TextMeshProUGUI testManaText;
    [SerializeField] TextMeshProUGUI testLvlText;
    [SerializeField] TextMeshProUGUI testStatsText;

    void Update()
    {
        currentHealth = playerHealth.CurrentHealth;
        maxHealth = playerHealth.MaxHealth;
        currentMana = playerMana.CurrentMana;
        maxMana = playerMana.MaxMana;
        currentLvl = expSystem.CurrentLvl;
        maxLvl = expSystem.MaxLvl;
        strength = statsSystem.Strength; 
        mana = statsSystem.Mana; 
        defence = statsSystem.Defence;

        testHealthText.text = "Здоровье: " + currentHealth + "/" + maxHealth;
        testManaText.text = "Мана: " + currentMana + "/" + maxMana;
        testLvlText.text = "Уровень: " + currentLvl + "/" + maxLvl;
        testStatsText.text = "Сила: " + strength + " Мана: " + mana + " Защита: " + defence;
    }
}
