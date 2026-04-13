using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    [Serializable]
    private class QuestRuntimeWrapper
    {
        public List<QuestRuntimeData> items = new List<QuestRuntimeData>();
    }

    public static SaveManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Screenshot")]
    [SerializeField] private Camera screenshotCamera;
    [SerializeField] private int thumbnailWidth = 320;
    [SerializeField] private int thumbnailHeight = 180;

    [Header("Slots")]
    [SerializeField] private int slotCount = 3;
    [SerializeField] private bool verboseLogs;

    private const int CurrentSaveVersion = 1;
    private static readonly BindingFlags BindingFlagsInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private GameSaveData pendingLoadData;

    public event Action<int> OnSlotChanged;

    public int SlotCount => Mathf.Max(1, slotCount);

    private string SavesFolderPath => Path.Combine(Application.persistentDataPath, "SaveSlots");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(SavesFolderPath);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public bool HasSave(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return false;

        return File.Exists(GetJsonPath(slotIndex));
    }

    public SaveSlotMetadata GetSlotMetadata(int slotIndex)
    {
        SaveSlotMetadata metadata = new SaveSlotMetadata();
        metadata.exists = false;

        if (!TryReadSaveData(slotIndex, out GameSaveData data))
            return metadata;

        metadata.exists = true;
        metadata.sceneName = data.sceneName;
        metadata.saveTimestampUtc = data.saveTimestampUtc;
        metadata.playerLevel = data.playerLevel;
        return metadata;
    }

    public Texture2D LoadThumbnail(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        string path = GetThumbnailPath(slotIndex);
        if (!File.Exists(path))
            return null;

        byte[] pngBytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(pngBytes, false);
        texture.name = "SaveSlotThumbnail_" + slotIndex;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    public bool SaveToSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            Debug.LogWarning("SaveManager: invalid slot index.");
            return false;
        }

        if (itemDatabase == null)
        {
            Debug.LogWarning("SaveManager: ItemDatabase is not assigned.");
            return false;
        }

        GameSaveData data = BuildCurrentSaveData();
        if (data == null)
            return false;

        Directory.CreateDirectory(SavesFolderPath);
        File.WriteAllText(GetJsonPath(slotIndex), JsonUtility.ToJson(data, true));

        byte[] thumbnailBytes = CaptureThumbnailPng();
        if (thumbnailBytes != null && thumbnailBytes.Length > 0)
            File.WriteAllBytes(GetThumbnailPath(slotIndex), thumbnailBytes);

        if (verboseLogs)
            Debug.Log("SaveManager: saved slot " + slotIndex);

        OnSlotChanged?.Invoke(slotIndex);
        return true;
    }

    public bool LoadFromSlot(int slotIndex)
    {
        if (!TryReadSaveData(slotIndex, out GameSaveData data))
            return false;

        pendingLoadData = data;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        Time.timeScale = 1f;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.LoadScene(data.sceneName);

        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadData == null)
            return;

        if (!string.Equals(scene.name, pendingLoadData.sceneName, StringComparison.Ordinal))
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StartCoroutine(ApplyLoadedDataRoutine());
    }

    private IEnumerator ApplyLoadedDataRoutine()
    {
        yield return null;
        yield return null;

        ApplyLoadedData(pendingLoadData);
        pendingLoadData = null;
    }

    private GameSaveData BuildCurrentSaveData()
    {
        GameSaveData data = new GameSaveData();
        data.version = CurrentSaveVersion;
        data.sceneName = SceneManager.GetActiveScene().name;
        data.saveTimestampUtc = DateTime.UtcNow.ToString("O");

        ExpSystem expSystem = FindFirstObjectByType<ExpSystem>();
        if (expSystem != null)
        {
            data.playerLevel = expSystem.CurrentLvl;
            data.playerProgress.currentLevel = expSystem.CurrentLvl;
            data.playerProgress.currentXp = expSystem.CurrentXP;
            data.playerProgress.xpToNextLevel = expSystem.XpToNextLvl;
        }
        else
        {
            data.playerLevel = 1;
            data.playerProgress.currentLevel = 1;
            data.playerProgress.currentXp = 0;
            data.playerProgress.xpToNextLevel = 100;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            data.playerPosition = new Vector3SaveData(player.transform.position);

        InventorySystem inventorySystem = InventorySystem.Instance;
        if (inventorySystem != null)
        {
            data.inventory.consumables = CaptureInventoryList(inventorySystem.ConsumableItems);
            data.inventory.questItems = CaptureInventoryList(inventorySystem.QuestItems);
            data.inventory.equipmentItems = CaptureInventoryList(inventorySystem.EquipmentItems);
        }

        EquipmentSystem equipmentSystem = EquipmentSystem.Instance;
        if (equipmentSystem != null)
        {
            for (int slotIndex = 0; slotIndex < equipmentSystem.SlotCount; slotIndex++)
            {
                ItemData item = equipmentSystem.GetItemInSlot(slotIndex);
                data.equipment.slotItemIds.Add(item != null ? item.ItemId : string.Empty);
            }
        }

        if (DialogueRuntimeState.Instance != null)
            data.dialogue = DialogueRuntimeState.Instance.CaptureState();

        if (QuestManager.Instance != null)
        {
            data.quests.activeQuests = CloneQuestList(QuestManager.Instance.ActiveQuests);
            data.quests.completedQuests = CloneQuestList(QuestManager.Instance.CompletedQuests);
            data.quests.failedQuests = CloneQuestList(QuestManager.Instance.FailedQuests);
        }

        return data;
    }

    private void ApplyLoadedData(GameSaveData data)
    {
        if (data == null)
            return;

        ApplyPlayerProgress(data.playerProgress);
        ApplyInventory(data.inventory);
        ApplyEquipment(data.equipment);
        ApplyDialogue(data.dialogue);
        ApplyQuests(data.quests);
        ApplyPlayerPosition(data.playerPosition);

        if (verboseLogs)
            Debug.Log("SaveManager: load applied for scene " + data.sceneName);
    }

    private void ApplyPlayerProgress(PlayerProgressSaveData progress)
    {
        ExpSystem expSystem = FindFirstObjectByType<ExpSystem>();
        if (expSystem == null || progress == null)
            return;

        expSystem.SetProgressFromSave(progress.currentLevel, progress.currentXp, progress.xpToNextLevel);
    }

    private void ApplyInventory(InventoryStateSaveData inventoryData)
    {
        InventorySystem inventorySystem = InventorySystem.Instance;
        if (inventorySystem == null)
            return;

        ClearInventorySection(inventorySystem, inventorySystem.ConsumableItems);
        ClearInventorySection(inventorySystem, inventorySystem.QuestItems);
        ClearInventorySection(inventorySystem, inventorySystem.EquipmentItems);

        if (inventoryData == null)
            return;

        RestoreInventoryList(inventorySystem, inventoryData.consumables);
        RestoreInventoryList(inventorySystem, inventoryData.questItems);
        RestoreInventoryList(inventorySystem, inventoryData.equipmentItems);
    }

    private void ApplyEquipment(EquipmentStateSaveData equipmentData)
    {
        EquipmentSystem equipmentSystem = EquipmentSystem.Instance;
        InventorySystem inventorySystem = InventorySystem.Instance;

        if (equipmentSystem == null || inventorySystem == null)
            return;

        for (int slotIndex = 0; slotIndex < equipmentSystem.SlotCount; slotIndex++)
            equipmentSystem.UnequipSlot(slotIndex);

        if (equipmentData == null || equipmentData.slotItemIds == null)
            return;

        int slotCountToRestore = Mathf.Min(equipmentSystem.SlotCount, equipmentData.slotItemIds.Count);

        for (int slotIndex = 0; slotIndex < slotCountToRestore; slotIndex++)
        {
            string itemId = equipmentData.slotItemIds[slotIndex];
            if (string.IsNullOrWhiteSpace(itemId))
                continue;

            ItemData item = itemDatabase.GetItemOrNull(itemId);
            if (item == null)
            {
                Debug.LogWarning("SaveManager: missing ItemData for equipped item id " + itemId);
                continue;
            }

            inventorySystem.AddItem(item, 1);
            equipmentSystem.EquipItemToSlot(item, slotIndex);
        }
    }

    private void ApplyDialogue(DialogueStateSaveData dialogueData)
    {
        if (DialogueRuntimeState.Instance == null)
            return;

        DialogueRuntimeState.Instance.RestoreState(dialogueData);
    }

    private void ApplyQuests(QuestStateSaveData questData)
    {
        QuestManager questManager = QuestManager.Instance;
        if (questManager == null)
            return;

        Dictionary<string, QuestRuntimeData> activeQuests = GetQuestDictionary(questManager, "activeQuests");
        Dictionary<string, QuestRuntimeData> completedQuests = GetQuestDictionary(questManager, "completedQuests");
        Dictionary<string, QuestRuntimeData> failedQuests = GetQuestDictionary(questManager, "failedQuests");

        if (activeQuests == null || completedQuests == null || failedQuests == null)
        {
            Debug.LogWarning("SaveManager: quest dictionaries were not found. Quest load skipped.");
            return;
        }

        RestoreQuestDictionary(activeQuests, questData != null ? questData.activeQuests : null);
        RestoreQuestDictionary(completedQuests, questData != null ? questData.completedQuests : null);
        RestoreQuestDictionary(failedQuests, questData != null ? questData.failedQuests : null);

        InvokePrivateMethod(questManager, "RefreshItemObjectivesForAllActiveQuests");
        InvokeQuestEvent(questManager, "OnPinnedQuestsChanged");
        InvokeQuestEvent(questManager, "OnQuestListChanged");
    }

    private void ApplyPlayerPosition(Vector3SaveData positionData)
    {
        if (positionData == null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        Rigidbody2D rigidbody2D = player.GetComponent<Rigidbody2D>();
        Vector3 targetPosition = positionData.ToVector3();

        if (rigidbody2D != null)
        {
            rigidbody2D.position = targetPosition;
            rigidbody2D.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = targetPosition;
        }
    }

    private List<ItemStackSaveData> CaptureInventoryList(IReadOnlyList<InventoryEntry> source)
    {
        List<ItemStackSaveData> result = new List<ItemStackSaveData>();
        Dictionary<string, int> amountByItemId = new Dictionary<string, int>();

        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            InventoryEntry entry = source[i];
            if (entry == null || entry.Item == null || string.IsNullOrWhiteSpace(entry.Item.ItemId) || entry.Amount <= 0)
                continue;

            if (!amountByItemId.ContainsKey(entry.Item.ItemId))
                amountByItemId.Add(entry.Item.ItemId, 0);

            amountByItemId[entry.Item.ItemId] += entry.Amount;
        }

        foreach (KeyValuePair<string, int> pair in amountByItemId)
        {
            ItemStackSaveData stack = new ItemStackSaveData();
            stack.itemId = pair.Key;
            stack.amount = pair.Value;
            result.Add(stack);
        }

        return result;
    }

    private void RestoreInventoryList(InventorySystem inventorySystem, List<ItemStackSaveData> source)
    {
        if (inventorySystem == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ItemStackSaveData entry = source[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.amount <= 0)
                continue;

            ItemData item = itemDatabase.GetItemOrNull(entry.itemId);
            if (item == null)
            {
                Debug.LogWarning("SaveManager: missing ItemData for inventory item id " + entry.itemId);
                continue;
            }

            inventorySystem.AddItem(item, entry.amount);
        }
    }

    private void ClearInventorySection(InventorySystem inventorySystem, IReadOnlyList<InventoryEntry> currentEntries)
    {
        if (inventorySystem == null || currentEntries == null || currentEntries.Count == 0)
            return;

        List<InventoryEntry> snapshot = new List<InventoryEntry>(currentEntries);

        for (int i = 0; i < snapshot.Count; i++)
        {
            InventoryEntry entry = snapshot[i];
            if (entry == null || entry.Item == null || entry.Amount <= 0)
                continue;

            inventorySystem.RemoveItem(entry.Item, entry.Amount);
        }
    }

    private List<QuestRuntimeData> CloneQuestList(IEnumerable<QuestRuntimeData> source)
    {
        List<QuestRuntimeData> result = new List<QuestRuntimeData>();
        if (source == null)
            return result;

        foreach (QuestRuntimeData runtimeData in source)
        {
            QuestRuntimeData clone = CloneQuestRuntime(runtimeData);
            if (clone != null)
                result.Add(clone);
        }

        return result;
    }

    private QuestRuntimeData CloneQuestRuntime(QuestRuntimeData source)
    {
        if (source == null)
            return null;

        QuestRuntimeWrapper wrapper = new QuestRuntimeWrapper();
        wrapper.items.Add(source);

        string json = JsonUtility.ToJson(wrapper);
        QuestRuntimeWrapper clonedWrapper = JsonUtility.FromJson<QuestRuntimeWrapper>(json);

        if (clonedWrapper == null || clonedWrapper.items == null || clonedWrapper.items.Count == 0)
            return null;

        return clonedWrapper.items[0];
    }

    private Dictionary<string, QuestRuntimeData> GetQuestDictionary(QuestManager questManager, string fieldName)
    {
        FieldInfo fieldInfo = typeof(QuestManager).GetField(fieldName, BindingFlagsInstance);
        if (fieldInfo == null)
            return null;

        return fieldInfo.GetValue(questManager) as Dictionary<string, QuestRuntimeData>;
    }

    private void RestoreQuestDictionary(Dictionary<string, QuestRuntimeData> target, List<QuestRuntimeData> source)
    {
        target.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            QuestRuntimeData clone = CloneQuestRuntime(source[i]);
            if (clone == null || string.IsNullOrWhiteSpace(clone.QuestId))
                continue;

            target[clone.QuestId] = clone;
        }
    }

    private void InvokePrivateMethod(object target, string methodName)
    {
        if (target == null)
            return;

        MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlagsInstance);
        if (methodInfo == null)
            return;

        methodInfo.Invoke(target, null);
    }

    private void InvokeQuestEvent(QuestManager questManager, string eventFieldName)
    {
        FieldInfo fieldInfo = typeof(QuestManager).GetField(eventFieldName, BindingFlagsInstance);
        if (fieldInfo == null)
            return;

        Delegate eventDelegate = fieldInfo.GetValue(questManager) as Delegate;
        if (eventDelegate == null)
            return;

        Delegate[] listeners = eventDelegate.GetInvocationList();
        for (int i = 0; i < listeners.Length; i++)
            listeners[i].DynamicInvoke();
    }

    private bool TryReadSaveData(int slotIndex, out GameSaveData data)
    {
        data = null;

        if (!IsValidSlot(slotIndex))
            return false;

        string path = GetJsonPath(slotIndex);
        if (!File.Exists(path))
            return false;

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        data = JsonUtility.FromJson<GameSaveData>(json);
        return data != null;
    }

    private byte[] CaptureThumbnailPng()
    {
        Camera targetCamera = screenshotCamera != null ? screenshotCamera : Camera.main;
        if (targetCamera == null)
        {
            Debug.LogWarning("SaveManager: no camera found for thumbnail capture.");
            return null;
        }

        int width = Mathf.Max(64, thumbnailWidth);
        int height = Mathf.Max(64, thumbnailHeight);

        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousCameraTarget = targetCamera.targetTexture;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        targetCamera.targetTexture = renderTexture;
        targetCamera.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        texture.Apply();

        targetCamera.targetTexture = previousCameraTarget;
        RenderTexture.active = previousActive;

        byte[] bytes = texture.EncodeToPNG();

        renderTexture.Release();
        Destroy(renderTexture);
        Destroy(texture);

        return bytes;
    }

    private string GetJsonPath(int slotIndex)
    {
        return Path.Combine(SavesFolderPath, "slot_" + slotIndex + ".json");
    }

    private string GetThumbnailPath(int slotIndex)
    {
        return Path.Combine(SavesFolderPath, "slot_" + slotIndex + ".png");
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }
}