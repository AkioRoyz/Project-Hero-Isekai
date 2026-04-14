using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class PauseMenuSaveAdapter : MonoBehaviour
{
    [Serializable]
    public class SceneDisplayNameEntry
    {
        public string sceneName;
        public string displayName;
    }

    [Serializable]
    public struct SlotPresentationData
    {
        public bool HasData;
        public string SceneName;
        public string DisplaySceneName;
        public string SaveDateText;
        public int PlayerLevel;
        public Sprite Thumbnail;
    }

    [Header("Save System Reference")]
    [SerializeField] private SaveManager saveManager;

    [Header("Slot Setup")]
    [SerializeField] private int managerSlotIndexOffset = 0;

    [Header("Scene Display Names")]
    [SerializeField] private SceneDisplayNameEntry[] sceneDisplayNames;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private readonly Dictionary<int, Texture2D> cachedTextures = new Dictionary<int, Texture2D>();
    private readonly Dictionary<int, Sprite> cachedSprites = new Dictionary<int, Sprite>();
    private bool isSubscribedToSaveManager;

    public SaveManager SaveManager => saveManager;

    public int SlotCount
    {
        get
        {
            if (saveManager == null)
            {
                return 0;
            }

            return saveManager.SlotCount;
        }
    }

    private void OnEnable()
    {
        SubscribeToSaveManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromSaveManager();
    }

    private void OnDestroy()
    {
        ClearThumbnailCache();
    }

    public bool TryGetSlotPresentation(int uiSlotIndex, out SlotPresentationData data)
    {
        data = default;

        if (saveManager == null)
        {
            return false;
        }

        int managerSlotIndex = ToManagerSlotIndex(uiSlotIndex);

        if (managerSlotIndex < 0 || managerSlotIndex >= saveManager.SlotCount)
        {
            return false;
        }

        SaveSlotMetadata metadata = saveManager.GetSlotMetadata(managerSlotIndex);
        Sprite thumbnailSprite = GetOrCreateThumbnailSprite(managerSlotIndex);

        data = new SlotPresentationData
        {
            HasData = metadata.exists,
            SceneName = metadata.sceneName,
            DisplaySceneName = ResolveSceneDisplayName(metadata.sceneName),
            SaveDateText = FormatSaveDate(metadata.saveTimestampUtc),
            PlayerLevel = metadata.playerLevel,
            Thumbnail = thumbnailSprite
        };

        return true;
    }

    public void SaveToSlot(int uiSlotIndex)
    {
        if (saveManager == null)
        {
            Debug.LogError("[PauseMenuSaveAdapter] SaveManager is not assigned.");
            return;
        }

        int managerSlotIndex = ToManagerSlotIndex(uiSlotIndex);

        if (verboseLogs)
        {
            Debug.Log($"[PauseMenuSaveAdapter] Saving to UI slot {uiSlotIndex}, manager slot {managerSlotIndex}.");
        }

        bool saveSucceeded = saveManager.SaveToSlot(managerSlotIndex);

        // Важно: меню перед сохранением уже закрыто, поэтому нельзя полагаться только на OnSlotChanged.
        // Сбрасываем кэш превью сразу вручную, чтобы при следующем открытии загрузился новый PNG.
        if (saveSucceeded)
        {
            InvalidateThumbnailCacheForSlot(managerSlotIndex);

            if (verboseLogs)
            {
                Debug.Log($"[PauseMenuSaveAdapter] Thumbnail cache invalidated for slot {managerSlotIndex} after save.");
            }
        }
    }

    public void LoadFromSlot(int uiSlotIndex)
    {
        if (saveManager == null)
        {
            Debug.LogError("[PauseMenuSaveAdapter] SaveManager is not assigned.");
            return;
        }

        int managerSlotIndex = ToManagerSlotIndex(uiSlotIndex);

        if (verboseLogs)
        {
            Debug.Log($"[PauseMenuSaveAdapter] Loading from UI slot {uiSlotIndex}, manager slot {managerSlotIndex}.");
        }

        saveManager.LoadFromSlot(managerSlotIndex);
    }

    private int ToManagerSlotIndex(int uiSlotIndex)
    {
        return uiSlotIndex + managerSlotIndexOffset;
    }

    private string ResolveSceneDisplayName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return string.Empty;
        }

        if (sceneDisplayNames != null)
        {
            for (int i = 0; i < sceneDisplayNames.Length; i++)
            {
                SceneDisplayNameEntry entry = sceneDisplayNames[i];
                if (entry != null && entry.sceneName == sceneName && !string.IsNullOrWhiteSpace(entry.displayName))
                {
                    return entry.displayName;
                }
            }
        }

        return sceneName;
    }

    private string FormatSaveDate(string rawUtcDate)
    {
        if (string.IsNullOrWhiteSpace(rawUtcDate))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(
                rawUtcDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsedUtc))
        {
            return parsedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }

        if (DateTime.TryParse(rawUtcDate, out DateTime parsedLocal))
        {
            return parsedLocal.ToString("dd.MM.yyyy HH:mm");
        }

        return rawUtcDate;
    }

    private Sprite GetOrCreateThumbnailSprite(int managerSlotIndex)
    {
        if (cachedSprites.TryGetValue(managerSlotIndex, out Sprite existingSprite) && existingSprite != null)
        {
            return existingSprite;
        }

        Texture2D texture = saveManager.LoadThumbnail(managerSlotIndex);
        if (texture == null)
        {
            return null;
        }

        cachedTextures[managerSlotIndex] = texture;

        Rect rect = new Rect(0f, 0f, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        Sprite createdSprite = Sprite.Create(texture, rect, pivot);
        createdSprite.name = $"PauseMenuThumbnail_{managerSlotIndex}";
        cachedSprites[managerSlotIndex] = createdSprite;

        return createdSprite;
    }

    private void SubscribeToSaveManager()
    {
        if (isSubscribedToSaveManager || saveManager == null)
        {
            return;
        }

        saveManager.OnSlotChanged += HandleSlotChanged;
        isSubscribedToSaveManager = true;
    }

    private void UnsubscribeFromSaveManager()
    {
        if (!isSubscribedToSaveManager || saveManager == null)
        {
            isSubscribedToSaveManager = false;
            return;
        }

        saveManager.OnSlotChanged -= HandleSlotChanged;
        isSubscribedToSaveManager = false;
    }

    private void HandleSlotChanged(int managerSlotIndex)
    {
        InvalidateThumbnailCacheForSlot(managerSlotIndex);
    }

    private void InvalidateThumbnailCacheForSlot(int managerSlotIndex)
    {
        if (cachedSprites.TryGetValue(managerSlotIndex, out Sprite sprite) && sprite != null)
        {
            Destroy(sprite);
        }

        if (cachedTextures.TryGetValue(managerSlotIndex, out Texture2D texture) && texture != null)
        {
            Destroy(texture);
        }

        cachedSprites.Remove(managerSlotIndex);
        cachedTextures.Remove(managerSlotIndex);
    }

    private void ClearThumbnailCache()
    {
        foreach (KeyValuePair<int, Sprite> pair in cachedSprites)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }

        foreach (KeyValuePair<int, Texture2D> pair in cachedTextures)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }

        cachedSprites.Clear();
        cachedTextures.Clear();
    }
}