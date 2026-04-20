using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Merchant_", menuName = "Game/Merchant/Merchant Data")]
public class MerchantData : ScriptableObject
{
    [SerializeField] private string merchantId;
    [SerializeField] private List<MerchantStockEntry> stockEntries = new();

    public string MerchantId => merchantId;
    public IReadOnlyList<MerchantStockEntry> StockEntries => stockEntries;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            merchantId = name;
    }
#endif
}