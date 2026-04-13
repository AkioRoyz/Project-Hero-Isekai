using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickConsumableUser : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMana playerMana;
    [SerializeField] private PlayerMoving playerMoving;
    [SerializeField] private StatsSystem statsSystem;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform teleportAnchor;

    [Header("Quick Slots 1-5 (order matters)")]
    [SerializeField] private ItemData[] quickSlotItems = new ItemData[5];

    private GameInput subscribedInput;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResolveReferences();
        RebindInput();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindInput();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();
        RebindInput();
    }

    private void ResolveReferences()
    {
        if (gameInput == null)
            gameInput = FindFirstObjectByType<GameInput>();

        if (inventorySystem == null)
            inventorySystem = InventorySystem.Instance != null
                ? InventorySystem.Instance
                : FindFirstObjectByType<InventorySystem>();

        if (statsSystem == null)
            statsSystem = FindFirstObjectByType<StatsSystem>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (playerTransform == null)
                playerTransform = player.transform;

            if (playerHealth == null)
                playerHealth = player.GetComponent<PlayerHealth>();

            if (playerMana == null)
                playerMana = player.GetComponent<PlayerMana>();

            if (playerMoving == null)
                playerMoving = player.GetComponent<PlayerMoving>();
        }
    }

    private void RebindInput()
    {
        UnbindInput();

        if (gameInput == null)
            return;

        gameInput.OnQuickSlotPressed += UseQuickSlotItem;
        subscribedInput = gameInput;
    }

    private void UnbindInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnQuickSlotPressed -= UseQuickSlotItem;
        subscribedInput = null;
    }

    private void UseQuickSlotItem(int slotNumber)
    {
        ResolveReferences();

        int index = slotNumber - 1;

        if (index < 0 || index >= quickSlotItems.Length)
            return;

        if (inventorySystem == null)
        {
            Debug.LogWarning("QuickConsumableUser: InventorySystem is missing.");
            return;
        }

        ItemData item = quickSlotItems[index];

        if (item == null)
        {
            Debug.LogWarning($"Quick slot {slotNumber} has no assigned item.");
            return;
        }

        if (item.ItemType != ItemType.Consumable)
        {
            Debug.LogWarning($"{item.name} is not a consumable item.");
            return;
        }

        if (!inventorySystem.HasItem(item, 1))
        {
            Debug.Log($"No item in inventory: {item.ItemId}");
            return;
        }

        bool wasUsed = TryApplyConsumable(item);

        if (wasUsed)
            inventorySystem.RemoveItem(item, 1);
    }

    private bool TryApplyConsumable(ItemData item)
    {
        switch (item.ConsumableEffectType)
        {
            case ConsumableEffectType.HealHealth:
                if (playerHealth == null) return false;
                playerHealth.Heal(item.ConsumableValue);
                return true;

            case ConsumableEffectType.RestoreMana:
                if (playerMana == null) return false;
                playerMana.RestoreMana(item.ConsumableValue);
                return true;

            case ConsumableEffectType.MoveSpeedBuff:
                if (playerMoving == null) return false;
                if (item.ConsumableDuration <= 0f) return false;

                playerMoving.AddTemporarySpeedBonus(item.ConsumableValue, item.ConsumableDuration);
                return true;

            case ConsumableEffectType.DefenceBuff:
                if (statsSystem == null) return false;
                if (item.ConsumableDuration <= 0f) return false;

                StartCoroutine(TemporaryDefenceBuffRoutine(item.ConsumableValue, item.ConsumableDuration));
                return true;

            case ConsumableEffectType.TeleportToAnchor:
                if (playerTransform == null || teleportAnchor == null)
                {
                    Debug.LogWarning("Teleport item requires Player Transform and Teleport Anchor.");
                    return false;
                }

                playerTransform.position = teleportAnchor.position;
                return true;

            default:
                Debug.LogWarning($"Unsupported consumable effect on item: {item.ItemId}");
                return false;
        }
    }

    private IEnumerator TemporaryDefenceBuffRoutine(int bonusDefence, float duration)
    {
        if (statsSystem == null)
            yield break;

        statsSystem.AddBonusStats(0, 0, bonusDefence);
        yield return new WaitForSeconds(duration);

        if (statsSystem != null)
            statsSystem.RemoveBonusStats(0, 0, bonusDefence);
    }
}