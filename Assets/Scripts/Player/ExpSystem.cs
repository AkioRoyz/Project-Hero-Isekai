using System;
using UnityEngine;

public class ExpSystem : MonoBehaviour
{
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

    private void Start()
    {
        OnXpAdd?.Invoke(currentXP);
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

    private void AddXPInternal(int amount)
    {
        if (amount <= 0)
            return;

        if (currentLvl == maxLvl)
            return;

        currentXP += amount;

        while (currentXP >= xpToNextLvl)
        {
            currentXP -= xpToNextLvl;
            LvlUp();

            if (currentLvl == maxLvl)
            {
                currentXP = xpToNextLvl;
                break;
            }
        }

        OnXpAdd?.Invoke(currentXP);
    }

    private void LvlUp()
    {
        if (currentLvl == maxLvl)
            return;

        currentLvl++;

        if (currentLvl < maxLvl)
        {
            xpToNextLvl = Mathf.RoundToInt(xpToNextLvl * (1 + percentUp / 100f));
        }

        OnLevelChange?.Invoke(currentLvl);
    }
}