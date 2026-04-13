using System;
using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [SerializeField] private int maxMana;
    [SerializeField] private int currentMana;
    [SerializeField] private StatsSystem statsSystem;

    public int MaxMana => maxMana;
    public int CurrentMana => currentMana;

    public event Action OnManaChange;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        RefreshManaFromStats(true);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (statsSystem != null)
        {
            statsSystem.OnStatsUpdate += OnStatsUpdated;
            statsSystem.OnLevelStatsUpdated += OnLevelStatsUpdated;
        }
    }

    private void OnDisable()
    {
        if (statsSystem != null)
        {
            statsSystem.OnStatsUpdate -= OnStatsUpdated;
            statsSystem.OnLevelStatsUpdated -= OnLevelStatsUpdated;
        }
    }

    private void ResolveReferences()
    {
        if (statsSystem == null)
            statsSystem = FindFirstObjectByType<StatsSystem>();
    }

    public bool SpendMana(int amount)
    {
        if (amount <= 0) return true;
        if (currentMana < amount) return false;

        currentMana -= amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        OnManaChange?.Invoke();
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;

        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        OnManaChange?.Invoke();
    }

    public void RestoreToFull()
    {
        currentMana = maxMana;
        OnManaChange?.Invoke();
    }

    private void OnStatsUpdated()
    {
        RefreshManaFromStats(false);
    }

    private void OnLevelStatsUpdated()
    {
        currentMana = maxMana;
        OnManaChange?.Invoke();
    }

    private void RefreshManaFromStats(bool firstInit)
    {
        if (statsSystem == null)
        {
            maxMana = Mathf.Max(maxMana, 1);
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            OnManaChange?.Invoke();
            return;
        }

        int oldMaxMana = maxMana;
        maxMana = Mathf.Max(1, statsSystem.Mana * 10);

        if (firstInit || oldMaxMana <= 0)
        {
            currentMana = maxMana;
        }
        else
        {
            int difference = maxMana - oldMaxMana;
            currentMana += difference;
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        }

        OnManaChange?.Invoke();
    }
}