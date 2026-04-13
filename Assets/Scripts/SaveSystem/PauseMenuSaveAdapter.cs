using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private MonoBehaviour saveManager;

    [Header("Slot Setup")]
    [SerializeField] private int fallbackSlotCount = 5;
    [SerializeField] private int managerSlotIndexOffset = 0;

    [Header("Scene Display Names")]
    [SerializeField] private SceneDisplayNameEntry[] sceneDisplayNames;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;

    public MonoBehaviour SaveManager => saveManager;

    public int SlotCount
    {
        get
        {
            int count = ReadSlotCountFromManager();
            return count > 0 ? count : fallbackSlotCount;
        }
    }

    public bool TryGetSlotPresentation(int uiSlotIndex, out SlotPresentationData data)
    {
        data = default;

        if (saveManager == null)
        {
            return false;
        }

        int managerSlotIndex = ToManagerSlotIndex(uiSlotIndex);

        object metadata = GetMetadata(managerSlotIndex);
        Sprite thumbnail = GetThumbnail(managerSlotIndex, metadata);

        bool hasData = DetermineHasData(metadata, thumbnail);

        string sceneName = ReadStringMember(metadata, "sceneName", "SceneName", "sceneId", "SceneId", "scene", "Scene");
        string saveDateText = BuildDateText(metadata);
        int playerLevel = ReadIntMember(metadata, "playerLevel", "PlayerLevel", "level", "Level");

        data = new SlotPresentationData
        {
            HasData = hasData,
            SceneName = sceneName,
            DisplaySceneName = ResolveSceneDisplayName(sceneName),
            SaveDateText = saveDateText,
            PlayerLevel = playerLevel,
            Thumbnail = thumbnail
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

        if (TryInvokeCandidateMethod(saveManager, new[]
            {
                "SaveToSlot",
                "SaveGameToSlot",
                "SaveSlot",
                "WriteSave",
                "CreateSaveForSlot"
            }, managerSlotIndex))
        {
            return;
        }

        if (TryInvokeCandidateMethod(saveManager, new[]
            {
                "Save",
                "SaveGame"
            }, managerSlotIndex))
        {
            return;
        }

        Debug.LogError($"[PauseMenuSaveAdapter] Could not find save method on {saveManager.GetType().Name}.");
    }

    public void LoadFromSlot(int uiSlotIndex)
    {
        if (saveManager == null)
        {
            Debug.LogError("[PauseMenuSaveAdapter] SaveManager is not assigned.");
            return;
        }

        int managerSlotIndex = ToManagerSlotIndex(uiSlotIndex);

        if (TryInvokeCandidateMethod(saveManager, new[]
            {
                "LoadFromSlot",
                "LoadGameFromSlot",
                "LoadSlot",
                "ReadSave",
                "LoadSaveFromSlot"
            }, managerSlotIndex))
        {
            return;
        }

        if (TryInvokeCandidateMethod(saveManager, new[]
            {
                "Load",
                "LoadGame"
            }, managerSlotIndex))
        {
            return;
        }

        Debug.LogError($"[PauseMenuSaveAdapter] Could not find load method on {saveManager.GetType().Name}.");
    }

    private int ToManagerSlotIndex(int uiSlotIndex)
    {
        return uiSlotIndex + managerSlotIndexOffset;
    }

    private int ReadSlotCountFromManager()
    {
        if (saveManager == null)
        {
            return 0;
        }

        object value = ReadMemberValue(saveManager,
            "slotCount",
            "SlotCount",
            "saveSlotCount",
            "SaveSlotCount");

        if (value != null)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                // ignored
            }
        }

        object methodResult = InvokeCandidateMethod(saveManager,
            new[]
            {
                "GetSlotCount",
                "GetSaveSlotCount"
            });

        if (methodResult != null)
        {
            try
            {
                return Convert.ToInt32(methodResult);
            }
            catch
            {
                // ignored
            }
        }

        return 0;
    }

    private object GetMetadata(int managerSlotIndex)
    {
        if (saveManager == null)
        {
            return null;
        }

        object methodResult = InvokeCandidateMethod(saveManager,
            new[]
            {
                "GetSlotMetadata",
                "GetSaveSlotMetadata",
                "GetSlotInfo",
                "GetSaveInfo",
                "GetSlotData",
                "GetPreviewData",
                "GetSavePreviewData"
            },
            managerSlotIndex);

        if (methodResult != null)
        {
            return methodResult;
        }

        object collection = ReadMemberValue(saveManager,
            "slotMetadata",
            "slotMetadatas",
            "saveSlots",
            "slots",
            "slotInfos",
            "saveInfos");

        if (collection is IList list && managerSlotIndex >= 0 && managerSlotIndex < list.Count)
        {
            return list[managerSlotIndex];
        }

        return null;
    }

    private Sprite GetThumbnail(int managerSlotIndex, object metadata)
    {
        object methodResult = InvokeCandidateMethod(saveManager,
            new[]
            {
                "LoadSlotThumbnail",
                "GetSlotThumbnail",
                "GetThumbnailForSlot",
                "LoadThumbnailForSlot",
                "GetSaveThumbnail"
            },
            managerSlotIndex);

        Sprite sprite = ConvertToSprite(methodResult);
        if (sprite != null)
        {
            return sprite;
        }

        object memberValue = ReadMemberValue(metadata,
            "thumbnail",
            "Thumbnail",
            "previewImage",
            "PreviewImage",
            "screenshot",
            "Screenshot");

        return ConvertToSprite(memberValue);
    }

    private bool DetermineHasData(object metadata, Sprite thumbnail)
    {
        if (metadata == null && thumbnail == null)
        {
            return false;
        }

        object hasDataValue = ReadMemberValue(metadata, "HasData", "hasData", "IsOccupied", "isOccupied");
        if (hasDataValue is bool hasDataBool)
        {
            return hasDataBool;
        }

        object isEmptyValue = ReadMemberValue(metadata, "IsEmpty", "isEmpty");
        if (isEmptyValue is bool isEmptyBool)
        {
            return !isEmptyBool;
        }

        string sceneName = ReadStringMember(metadata, "sceneName", "SceneName", "sceneId", "SceneId", "scene", "Scene");
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            return true;
        }

        string dateText = BuildDateText(metadata);
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            return true;
        }

        if (thumbnail != null)
        {
            return true;
        }

        return false;
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

    private string BuildDateText(object metadata)
    {
        object rawDate = ReadMemberValue(metadata,
            "saveTimestampUtc",
            "SaveTimestampUtc",
            "saveTimeUtc",
            "SaveTimeUtc",
            "savedAtUtc",
            "SavedAtUtc",
            "timestampUtc",
            "TimestampUtc",
            "savedAt",
            "SavedAt",
            "saveDate",
            "SaveDate");

        if (rawDate == null)
        {
            return string.Empty;
        }

        if (rawDate is DateTime dateTime)
        {
            return dateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }

        if (rawDate is string dateString)
        {
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedUtc))
            {
                return parsedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }

            if (DateTime.TryParse(dateString, out DateTime parsedLocal))
            {
                return parsedLocal.ToString("dd.MM.yyyy HH:mm");
            }

            return dateString;
        }

        try
        {
            long numeric = Convert.ToInt64(rawDate);

            if (numeric > 1000000000000L)
            {
                DateTime unixMilliseconds = DateTimeOffset.FromUnixTimeMilliseconds(numeric).LocalDateTime;
                return unixMilliseconds.ToString("dd.MM.yyyy HH:mm");
            }

            if (numeric > 1000000000L)
            {
                DateTime unixSeconds = DateTimeOffset.FromUnixTimeSeconds(numeric).LocalDateTime;
                return unixSeconds.ToString("dd.MM.yyyy HH:mm");
            }

            if (numeric > 1000000L)
            {
                DateTime ticksDate = new DateTime(numeric, DateTimeKind.Utc).ToLocalTime();
                return ticksDate.ToString("dd.MM.yyyy HH:mm");
            }
        }
        catch
        {
            // ignored
        }

        return rawDate.ToString();
    }

    private int ReadIntMember(object target, params string[] memberNames)
    {
        object value = ReadMemberValue(target, memberNames);

        if (value == null)
        {
            return 0;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }

    private string ReadStringMember(object target, params string[] memberNames)
    {
        object value = ReadMemberValue(target, memberNames);
        return value?.ToString() ?? string.Empty;
    }

    private object ReadMemberValue(object target, params string[] memberNames)
    {
        if (target == null)
        {
            return null;
        }

        Type type = target.GetType();

        for (int i = 0; i < memberNames.Length; i++)
        {
            string memberName = memberNames[i];

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(target);
            }
        }

        return null;
    }

    private object InvokeCandidateMethod(object target, string[] methodNames, params object[] args)
    {
        if (target == null)
        {
            return null;
        }

        Type type = target.GetType();
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methodNames.Length; i++)
        {
            string methodName = methodNames[i];

            for (int j = 0; j < methods.Length; j++)
            {
                MethodInfo method = methods[j];

                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                try
                {
                    return method.Invoke(target, args);
                }
                catch (Exception ex)
                {
                    if (verboseLogs)
                    {
                        Debug.LogWarning($"[PauseMenuSaveAdapter] Failed to invoke {method.Name}: {ex.Message}");
                    }
                }
            }
        }

        return null;
    }

    private bool TryInvokeCandidateMethod(object target, string[] methodNames, params object[] args)
    {
        if (target == null)
        {
            return false;
        }

        Type type = target.GetType();
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methodNames.Length; i++)
        {
            string methodName = methodNames[i];

            for (int j = 0; j < methods.Length; j++)
            {
                MethodInfo method = methods[j];

                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                try
                {
                    method.Invoke(target, args);
                    return true;
                }
                catch (Exception ex)
                {
                    if (verboseLogs)
                    {
                        Debug.LogWarning($"[PauseMenuSaveAdapter] Failed to invoke {method.Name}: {ex.Message}");
                    }
                }
            }
        }

        return false;
    }

    private Sprite ConvertToSprite(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is Sprite sprite)
        {
            return sprite;
        }

        if (value is Texture2D texture)
        {
            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, rect, pivot);
        }

        if (value is RenderTexture renderTexture)
        {
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = previous;

            Rect rect = new Rect(0f, 0f, texture2D.width, texture2D.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture2D, rect, pivot);
        }

        return null;
    }
}