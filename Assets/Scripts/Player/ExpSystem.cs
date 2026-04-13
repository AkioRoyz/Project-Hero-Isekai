using System;
using UnityEngine;

public class ExpSystem : MonoBehaviour
{
    public static ExpSystem Instance { get; private set; }

    [SerializeField] private int xpToNextLvl = 100;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int maxLvl = 50;
    [SerializeField] private int currentLvl = 1;
    [SerializeField] private int percentUp = 10;

    [Header("Debug")]
    [SerializeField] private int debugAddXpAmount = 100;

    public event Action<int> OnLevelChange;
    public event Action<int> OnXpAdd;

    public int XpToNextLvl => xpToNextLvl;
    public int CurrentLvl => currentLvl;
    public int MaxLvl => maxLvl;
    public int CurrentXP => currentXP;

    private void Awake()
    {
        Instance = this;
        ClampState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        BroadcastProgress();
    }

    private void OnEnable()
    {
        RewardSystem.OnRewardGiven += AddRewardXP;
    }

    private void OnDisable()
    {
        RewardSystem.OnRewardGiven -= AddRewardXP;
    }

    private void AddRewardXP(RewardData reward)
    {
        if (reward.Exp <= 0)
            return;

        AddXPInternal(reward.Exp);
    }

    public void AddXP(int amount)
    {
        AddXPInternal(amount);
    }

    [ContextMenu("Debug/Add XP")]
    private void DebugAddXpFromContextMenu()
    {
        AddXPInternal(debugAddXpAmount);
    }

    public void SetProgressFromSave(int level, int xp, int xpNeededForNextLevel)
    {
        maxLvl = Mathf.Max(1, maxLvl);
        currentLvl = Mathf.Clamp(level, 1, maxLvl);
        xpToNextLvl = Mathf.Max(1, xpNeededForNextLevel);
        currentXP = Mathf.Clamp(xp, 0, xpToNextLvl);

        if (currentLvl >= maxLvl)
            currentXP = Mathf.Clamp(currentXP, 0, xpToNextLvl);

        BroadcastProgress();
    }

    private void AddXPInternal(int amount)
    {
        ClampState();

        if (amount <= 0 || currentLvl >= maxLvl)
            return;

        currentXP += amount;

        while (currentLvl < maxLvl && currentXP >= xpToNextLvl)
        {
            currentXP -= xpToNextLvl;
            LevelUp();

            if (currentLvl >= maxLvl)
            {
                currentXP = xpToNextLvl;
                break;
            }
        }

        OnXpAdd?.Invoke(currentXP);
    }

    private void LevelUp()
    {
        if (currentLvl >= maxLvl)
            return;

        currentLvl++;

        int nextDelta = Mathf.Max(1, Mathf.CeilToInt(xpToNextLvl * (percentUp / 100f)));
        xpToNextLvl += nextDelta;

        OnLevelChange?.Invoke(currentLvl);
    }

    private void BroadcastProgress()
    {
        ClampState();
        OnLevelChange?.Invoke(currentLvl);
        OnXpAdd?.Invoke(currentXP);
    }

    private void ClampState()
    {
        maxLvl = Mathf.Max(1, maxLvl);
        currentLvl = Mathf.Clamp(currentLvl, 1, maxLvl);
        xpToNextLvl = Mathf.Max(1, xpToNextLvl);
        currentXP = Mathf.Clamp(currentXP, 0, xpToNextLvl);
    }
}