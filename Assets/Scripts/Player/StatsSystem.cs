using System;
using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseStrength = 15;
    [SerializeField] private int baseMana = 7;
    [SerializeField] private int baseDefence = 5;

    [Header("Per Level Bonus")]
    [SerializeField] private int strengthStep = 5;
    [SerializeField] private int manaStep = 4;
    [SerializeField] private int defenceStep = 3;

    [Header("References")]
    [SerializeField] private ExpSystem expSystem;

    private int bonusStrength;
    private int bonusMana;
    private int bonusDefence;

    private int currentStrength;
    private int currentMana;
    private int currentDefence;

    public event Action OnStatsUpdate;

    // Отдельное событие: статы были пересчитаны именно из-за повышения уровня
    public event Action OnLevelStatsUpdated;

    public int Strength => currentStrength;
    public int Mana => currentMana;
    public int Defence => currentDefence;

    private void Awake()
    {
        RecalculateStats();
    }

    private void OnEnable()
    {
        if (expSystem != null)
        {
            expSystem.OnLevelChange += OnLevelChanged;
        }
    }

    private void OnDisable()
    {
        if (expSystem != null)
        {
            expSystem.OnLevelChange -= OnLevelChanged;
        }
    }

    private void OnLevelChanged(int level)
    {
        RecalculateStats();
        OnLevelStatsUpdated?.Invoke();
    }

    public void AddBonusStats(int strength, int mana, int defence)
    {
        bonusStrength += strength;
        bonusMana += mana;
        bonusDefence += defence;

        RecalculateStats();
    }

    public void RemoveBonusStats(int strength, int mana, int defence)
    {
        bonusStrength -= strength;
        bonusMana -= mana;
        bonusDefence -= defence;

        RecalculateStats();
    }

    private void RecalculateStats()
    {
        int currentLevel = 1;

        if (expSystem != null)
        {
            currentLevel = expSystem.CurrentLvl;
        }

        currentStrength = baseStrength + strengthStep * (currentLevel - 1) + bonusStrength;
        currentMana = baseMana + manaStep * (currentLevel - 1) + bonusMana;
        currentDefence = baseDefence + defenceStep * (currentLevel - 1) + bonusDefence;

        currentStrength = Mathf.Max(1, currentStrength);
        currentMana = Mathf.Max(1, currentMana);
        currentDefence = Mathf.Max(0, currentDefence);

        OnStatsUpdate?.Invoke();
    }
}