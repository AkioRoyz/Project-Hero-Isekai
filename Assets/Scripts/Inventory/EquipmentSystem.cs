using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance { get; private set; }

    public event Action OnEquipmentChanged;

    [SerializeField] private StatsSystem statsSystem;
    [SerializeField] private int slotCount = 9;

    private ItemData[] equippedItems;

    public int SlotCount => slotCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate EquipmentSystem was destroyed.", gameObject);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureInitialized();
        ResolveStatsSystem(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Если StatsSystem был на старом объекте сцены и пропал,
        // находим новый и заново навешиваем бонусы от уже надетых предметов.
        if (statsSystem == null)
        {
            ResolveStatsSystem(true);
        }

        OnEquipmentChanged?.Invoke();
    }

    private void EnsureInitialized()
    {
        if (equippedItems != null && equippedItems.Length == slotCount)
            return;

        ItemData[] oldItems = equippedItems;
        equippedItems = new ItemData[slotCount];

        if (oldItems == null)
            return;

        int copyCount = Mathf.Min(oldItems.Length, equippedItems.Length);
        for (int i = 0; i < copyCount; i++)
        {
            equippedItems[i] = oldItems[i];
        }
    }

    private void ResolveStatsSystem(bool reapplyBonuses)
    {
        if (statsSystem != null)
            return;

        statsSystem = FindFirstObjectByType<StatsSystem>();

        if (statsSystem != null && reapplyBonuses)
        {
            ReapplyAllBonuses();
        }
    }

    private void ReapplyAllBonuses()
    {
        if (statsSystem == null || equippedItems == null)
            return;

        for (int i = 0; i < equippedItems.Length; i++)
        {
            ItemData item = equippedItems[i];
            if (item == null)
                continue;

            ApplyItemBonuses(item);
        }
    }

    public ItemData GetItemInSlot(int slotIndex)
    {
        EnsureInitialized();

        if (slotIndex < 0 || slotIndex >= equippedItems.Length)
            return null;

        return equippedItems[slotIndex];
    }

    public bool EquipItemToSlot(ItemData item, int slotIndex)
    {
        EnsureInitialized();
        ResolveStatsSystem(false);

        if (item == null)
        {
            Debug.LogWarning("EquipItemToSlot called with null item.");
            return false;
        }

        if (item.ItemType != ItemType.Equipment)
        {
            Debug.LogWarning($"{item.name} is not an equipment item.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= equippedItems.Length)
        {
            Debug.LogWarning($"Invalid equipment slot index: {slotIndex}");
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("InventorySystem.Instance is missing.");
            return false;
        }

        bool removedFromInventory = InventorySystem.Instance.RemoveItem(item, 1);
        if (!removedFromInventory)
        {
            Debug.LogWarning($"Could not remove item from inventory: {item.ItemId}");
            return false;
        }

        ItemData oldItem = equippedItems[slotIndex];

        if (oldItem != null)
        {
            RemoveItemBonuses(oldItem);
            InventorySystem.Instance.AddItem(oldItem, 1);
        }

        equippedItems[slotIndex] = item;
        ApplyItemBonuses(item);

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool UnequipSlot(int slotIndex)
    {
        EnsureInitialized();
        ResolveStatsSystem(false);

        if (slotIndex < 0 || slotIndex >= equippedItems.Length)
        {
            Debug.LogWarning($"Invalid equipment slot index: {slotIndex}");
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("InventorySystem.Instance is missing.");
            return false;
        }

        ItemData item = equippedItems[slotIndex];
        if (item == null)
            return false;

        RemoveItemBonuses(item);
        InventorySystem.Instance.AddItem(item, 1);
        equippedItems[slotIndex] = null;

        OnEquipmentChanged?.Invoke();
        return true;
    }

    private void ApplyItemBonuses(ItemData item)
    {
        if (statsSystem == null || item == null)
            return;

        statsSystem.AddBonusStats(
            item.EquipmentStrengthBonus,
            item.EquipmentManaBonus,
            item.EquipmentDefenceBonus
        );
    }

    private void RemoveItemBonuses(ItemData item)
    {
        if (statsSystem == null || item == null)
            return;

        statsSystem.RemoveBonusStats(
            item.EquipmentStrengthBonus,
            item.EquipmentManaBonus,
            item.EquipmentDefenceBonus
        );
    }
}