using System;
using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseStrength = 15;
    [SerializeField] private int baseMana = 7;
    [SerializeField] private int baseDefence = 5;
    [SerializeField] private int baseDexterity = 5;

    [Header("Per Level Bonus")]
    [SerializeField] private int strengthStep = 5;
    [SerializeField] private int manaStep = 4;
    [SerializeField] private int defenceStep = 3;
    [SerializeField] private int dexterityStep = 2;

    [Header("Critical Settings")]
    [Tooltip("Базовый шанс крита в процентах.")]
    [SerializeField] private float baseCriticalChancePercent = 5f;

    [Tooltip("Сколько процентов шанса крита даёт 1 единица ловкости.")]
    [SerializeField] private float criticalChancePerDexterity = 0.5f;

    [Tooltip("Максимальный шанс крита в процентах.")]
    [SerializeField] private float maxCriticalChancePercent = 75f;

    [Tooltip("Базовый множитель критического урона. 1.5 = 150% урона.")]
    [SerializeField] private float baseCriticalDamageMultiplier = 1.5f;

    [Tooltip("Сколько множителя критического урона даёт 1 единица ловкости. 0.02 = +2% урона.")]
    [SerializeField] private float criticalDamagePerDexterity = 0.02f;

    [Tooltip("Максимальный множитель критического урона.")]
    [SerializeField] private float maxCriticalDamageMultiplier = 3f;

    [Header("References")]
    [SerializeField] private ExpSystem expSystem;

    private int bonusStrength;
    private int bonusMana;
    private int bonusDefence;
    private int bonusDexterity;

    private int currentStrength;
    private int currentMana;
    private int currentDefence;
    private int currentDexterity;

    public event Action OnStatsUpdate;

    // Отдельное событие: статы были пересчитаны именно из-за повышения уровня
    public event Action OnLevelStatsUpdated;

    public int Strength => currentStrength;
    public int Mana => currentMana;
    public int Defence => currentDefence;
    public int Dexterity => currentDexterity;

    /// <summary>
    /// Шанс критического удара в процентах.
    /// Например: 25 = 25%.
    /// </summary>
    public float CriticalChancePercent
    {
        get
        {
            float value = baseCriticalChancePercent + currentDexterity * criticalChancePerDexterity;
            return Mathf.Clamp(value, 0f, maxCriticalChancePercent);
        }
    }

    /// <summary>
    /// Шанс критического удара в формате 0-1.
    /// Например: 0.25 = 25%.
    /// Удобно использовать в Random.value.
    /// </summary>
    public float CriticalChance01 => CriticalChancePercent / 100f;

    /// <summary>
    /// Множитель критического урона.
    /// Например: 1.5 = 150% урона.
    /// </summary>
    public float CriticalDamageMultiplier
    {
        get
        {
            float value = baseCriticalDamageMultiplier + currentDexterity * criticalDamagePerDexterity;
            return Mathf.Clamp(value, 1f, maxCriticalDamageMultiplier);
        }
    }

    /// <summary>
    /// Критический урон в процентах для отображения в UI.
    /// Например: 150 = 150%.
    /// </summary>
    public float CriticalDamagePercent => CriticalDamageMultiplier * 100f;

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

    public void AddBonusStats(int strength, int mana, int defence, int dexterity)
    {
        bonusStrength += strength;
        bonusMana += mana;
        bonusDefence += defence;
        bonusDexterity += dexterity;

        RecalculateStats();
    }

    public void RemoveBonusStats(int strength, int mana, int defence, int dexterity)
    {
        bonusStrength -= strength;
        bonusMana -= mana;
        bonusDefence -= defence;
        bonusDexterity -= dexterity;

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
        currentDexterity = baseDexterity + dexterityStep * (currentLevel - 1) + bonusDexterity;

        currentStrength = Mathf.Max(1, currentStrength);
        currentMana = Mathf.Max(1, currentMana);
        currentDefence = Mathf.Max(0, currentDefence);
        currentDexterity = Mathf.Max(0, currentDexterity);

        OnStatsUpdate?.Invoke();
    }
}