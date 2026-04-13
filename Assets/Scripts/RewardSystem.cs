using System;
using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    public static RewardSystem Instance;

    public static event Action<RewardData> OnRewardGiven;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void GiveReward(RewardData reward)
    {
        OnRewardGiven?.Invoke(reward);
    }

    // Удобный перегруженный метод:
    // можно выдать только опыт/золото без предметов
    public void GiveReward(int exp, int gold)
    {
        RewardData reward = new RewardData(exp, gold);
        GiveReward(reward);
    }

    // Можно выдать опыт/золото + предметы
    public void GiveReward(int exp, int gold, RewardItemData[] items)
    {
        RewardData reward = new RewardData(exp, gold, items);
        GiveReward(reward);
    }
}