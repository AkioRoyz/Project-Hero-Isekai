using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class Vector3SaveData
{
    public float x;
    public float y;
    public float z;

    public Vector3SaveData()
    {
    }

    public Vector3SaveData(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class ItemStackSaveData
{
    public string itemId;
    public int amount;
}

[Serializable]
public class InventoryStateSaveData
{
    public List<ItemStackSaveData> consumables = new List<ItemStackSaveData>();
    public List<ItemStackSaveData> questItems = new List<ItemStackSaveData>();
    public List<ItemStackSaveData> equipmentItems = new List<ItemStackSaveData>();
}

[Serializable]
public class EquipmentStateSaveData
{
    public List<string> slotItemIds = new List<string>();
}

[Serializable]
public class PlayerProgressSaveData
{
    public int currentLevel;
    public int currentXp;
    public int xpToNextLevel;
}

[Serializable]
public class DialogueStateSaveData
{
    public List<string> playedKeys = new List<string>();
    public List<string> completedDialogueIds = new List<string>();
}

[Serializable]
public class QuestStateSaveData
{
    public List<QuestRuntimeData> activeQuests = new List<QuestRuntimeData>();
    public List<QuestRuntimeData> completedQuests = new List<QuestRuntimeData>();
    public List<QuestRuntimeData> failedQuests = new List<QuestRuntimeData>();
}

[Serializable]
public class MerchantStockRemainingSaveData
{
    public string merchantId;
    public string itemId;
    public int remainingAmount;
}

[Serializable]
public class MerchantRuntimeStateSaveData
{
    public List<MerchantStockRemainingSaveData> remainingStocks = new List<MerchantStockRemainingSaveData>();
}

[Serializable]
public class GameSaveData
{
    public int version = 2;
    public string sceneName;
    public string saveTimestampUtc;
    public int playerLevel;
    public Vector3SaveData playerPosition = new Vector3SaveData();
    public PlayerProgressSaveData playerProgress = new PlayerProgressSaveData();
    public InventoryStateSaveData inventory = new InventoryStateSaveData();
    public EquipmentStateSaveData equipment = new EquipmentStateSaveData();
    public DialogueStateSaveData dialogue = new DialogueStateSaveData();
    public QuestStateSaveData quests = new QuestStateSaveData();
    public MerchantRuntimeStateSaveData merchants = new MerchantRuntimeStateSaveData();
}

[Serializable]
public class SaveSlotMetadata
{
    public bool exists;
    public string sceneName;
    public string saveTimestampUtc;
    public int playerLevel;

    public string GetLocalTimeText()
    {
        if (string.IsNullOrWhiteSpace(saveTimestampUtc))
            return "--";

        if (DateTime.TryParse(saveTimestampUtc, null, DateTimeStyles.RoundtripKind, out DateTime parsed))
            return parsed.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        return saveTimestampUtc;
    }
}