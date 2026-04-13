using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    public event Action OnInventoryChanged;

    [Header("Runtime Lists")]
    [SerializeField] private List<InventoryEntry> consumableItems = new();
    [SerializeField] private List<InventoryEntry> questItems = new();
    [SerializeField] private List<InventoryEntry> equipmentItems = new();

    public IReadOnlyList<InventoryEntry> ConsumableItems => consumableItems;
    public IReadOnlyList<InventoryEntry> QuestItems => questItems;
    public IReadOnlyList<InventoryEntry> EquipmentItems => equipmentItems;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        RewardSystem.OnRewardGiven += HandleRewardGiven;
    }

    private void OnDisable()
    {
        RewardSystem.OnRewardGiven -= HandleRewardGiven;
    }

    private void HandleRewardGiven(RewardData reward)
    {
        if (reward.Items == null || reward.Items.Length == 0)
            return;

        for (int i = 0; i < reward.Items.Length; i++)
        {
            RewardItemData rewardItem = reward.Items[i];

            if (rewardItem.Item == null)
            {
                Debug.LogWarning("RewardSystem tried to give a null item.");
                continue;
            }

            if (rewardItem.Amount <= 0)
            {
                Debug.LogWarning($"Item reward amount for {rewardItem.Item.name} is <= 0.");
                continue;
            }

            AddItem(rewardItem.Item, rewardItem.Amount);
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null)
        {
            Debug.LogWarning("AddItem called with null ItemData.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"AddItem called with invalid amount: {amount}");
            return;
        }

        List<InventoryEntry> targetList = GetTargetList(item.ItemType);

        if (item.Stackable)
        {
            InventoryEntry existingEntry = FindEntry(targetList, item);

            if (existingEntry != null)
            {
                existingEntry.Amount += amount;
            }
            else
            {
                targetList.Add(new InventoryEntry(item, amount));
            }
        }
        else
        {
            // Если предмет не стакается, создаём отдельные записи
            for (int i = 0; i < amount; i++)
            {
                targetList.Add(new InventoryEntry(item, 1));
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null)
        {
            Debug.LogWarning("RemoveItem called with null ItemData.");
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning($"RemoveItem called with invalid amount: {amount}");
            return false;
        }

        List<InventoryEntry> targetList = GetTargetList(item.ItemType);

        if (item.Stackable)
        {
            InventoryEntry existingEntry = FindEntry(targetList, item);

            if (existingEntry == null || existingEntry.Amount < amount)
                return false;

            existingEntry.Amount -= amount;

            if (existingEntry.Amount <= 0)
            {
                targetList.Remove(existingEntry);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            int removedCount = 0;

            for (int i = targetList.Count - 1; i >= 0; i--)
            {
                if (targetList[i].Item == item)
                {
                    targetList.RemoveAt(i);
                    removedCount++;

                    if (removedCount >= amount)
                        break;
                }
            }

            if (removedCount < amount)
                return false;

            OnInventoryChanged?.Invoke();
            return true;
        }
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        List<InventoryEntry> targetList = GetTargetList(item.ItemType);

        int count = 0;

        for (int i = 0; i < targetList.Count; i++)
        {
            if (targetList[i].Item == item)
            {
                count += targetList[i].Amount;
            }
        }

        return count;
    }

    private List<InventoryEntry> GetTargetList(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Consumable:
                return consumableItems;

            case ItemType.Quest:
                return questItems;

            case ItemType.Equipment:
                return equipmentItems;

            default:
                Debug.LogWarning($"Unknown item type: {itemType}");
                return consumableItems;
        }
    }

    private InventoryEntry FindEntry(List<InventoryEntry> list, ItemData item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Item == item)
                return list[i];
        }

        return null;
    }
}