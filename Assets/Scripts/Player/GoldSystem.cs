using System;
using UnityEngine;

public class GoldSystem : MonoBehaviour
{
    [Header("Gold Item")]
    [SerializeField] private ItemData goldItem;

    public event Action OnGoldChange;

    public int GoldAmount
    {
        get
        {
            if (InventorySystem.Instance == null || goldItem == null)
                return 0;

            return InventorySystem.Instance.GetItemCount(goldItem);
        }
    }

    private void OnEnable()
    {
        RewardSystem.OnRewardGiven += GoldRewardAdd;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        RewardSystem.OnRewardGiven -= GoldRewardAdd;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void Start()
    {
        OnGoldChange?.Invoke();
    }

    private void HandleInventoryChanged()
    {
        OnGoldChange?.Invoke();
    }

    private void GoldRewardAdd(RewardData reward)
    {
        if (reward.Gold <= 0)
            return;

        AddGold(reward.Gold);
    }

    public bool HasEnoughGold(int amount)
    {
        if (amount <= 0)
            return true;

        return GoldAmount >= amount;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("GoldSystem: InventorySystem.Instance is missing.");
            return;
        }

        if (goldItem == null)
        {
            Debug.LogWarning("GoldSystem: goldItem is missing.");
            return;
        }

        InventorySystem.Instance.AddItem(goldItem, amount);
        OnGoldChange?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("GoldSystem: InventorySystem.Instance is missing.");
            return false;
        }

        if (goldItem == null)
        {
            Debug.LogWarning("GoldSystem: goldItem is missing.");
            return false;
        }

        if (!HasEnoughGold(amount))
            return false;

        bool removed = InventorySystem.Instance.RemoveItem(goldItem, amount);

        if (removed)
        {
            OnGoldChange?.Invoke();
        }

        return removed;
    }
}