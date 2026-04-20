using System.Collections.Generic;
using UnityEngine;

public static class MerchantRuntimeState
{
    private const string KeySeparator = "::";

    private static readonly Dictionary<string, int> remainingStockByKey = new();

    public static int GetRemainingStock(MerchantData merchantData, MerchantStockEntry stockEntry)
    {
        if (merchantData == null || stockEntry == null || stockEntry.Item == null)
            return 0;

        if (stockEntry.IsInfinite)
            return int.MaxValue;

        string key = BuildKey(merchantData, stockEntry);

        if (!remainingStockByKey.TryGetValue(key, out int remaining))
        {
            remaining = stockEntry.LimitedAmount;
            remainingStockByKey[key] = remaining;
        }

        return remaining;
    }

    public static bool TryConsumeOne(MerchantData merchantData, MerchantStockEntry stockEntry)
    {
        if (merchantData == null || stockEntry == null || stockEntry.Item == null)
            return false;

        if (stockEntry.IsInfinite)
            return true;

        string key = BuildKey(merchantData, stockEntry);
        int remaining = GetRemainingStock(merchantData, stockEntry);

        if (remaining <= 0)
            return false;

        remainingStockByKey[key] = remaining - 1;
        return true;
    }

    public static MerchantRuntimeStateSaveData CaptureState()
    {
        MerchantRuntimeStateSaveData data = new MerchantRuntimeStateSaveData();

        foreach (KeyValuePair<string, int> pair in remainingStockByKey)
        {
            SplitKey(pair.Key, out string merchantId, out string itemId);

            if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(itemId))
                continue;

            MerchantStockRemainingSaveData entry = new MerchantStockRemainingSaveData();
            entry.merchantId = merchantId;
            entry.itemId = itemId;
            entry.remainingAmount = Mathf.Max(0, pair.Value);
            data.remainingStocks.Add(entry);
        }

        return data;
    }

    public static void RestoreState(MerchantRuntimeStateSaveData data)
    {
        remainingStockByKey.Clear();

        if (data == null || data.remainingStocks == null)
            return;

        for (int i = 0; i < data.remainingStocks.Count; i++)
        {
            MerchantStockRemainingSaveData entry = data.remainingStocks[i];
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.merchantId) || string.IsNullOrWhiteSpace(entry.itemId))
                continue;

            string key = BuildKey(entry.merchantId, entry.itemId);
            remainingStockByKey[key] = Mathf.Max(0, entry.remainingAmount);
        }
    }

    public static void ClearAll()
    {
        remainingStockByKey.Clear();
    }

    private static string BuildKey(MerchantData merchantData, MerchantStockEntry stockEntry)
    {
        return BuildKey(GetMerchantId(merchantData), GetItemId(stockEntry.Item));
    }

    private static string BuildKey(string merchantId, string itemId)
    {
        return merchantId + KeySeparator + itemId;
    }

    private static void SplitKey(string key, out string merchantId, out string itemId)
    {
        merchantId = string.Empty;
        itemId = string.Empty;

        if (string.IsNullOrWhiteSpace(key))
            return;

        int separatorIndex = key.IndexOf(KeySeparator);
        if (separatorIndex < 0)
            return;

        merchantId = key.Substring(0, separatorIndex);
        itemId = key.Substring(separatorIndex + KeySeparator.Length);
    }

    private static string GetMerchantId(MerchantData merchantData)
    {
        if (merchantData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(merchantData.MerchantId))
            return merchantData.MerchantId;

        return merchantData.name;
    }

    private static string GetItemId(ItemData item)
    {
        if (item == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(item.ItemId))
            return item.ItemId;

        return item.name;
    }
}