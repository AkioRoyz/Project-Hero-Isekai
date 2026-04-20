using System;
using UnityEngine;

[Serializable]
public class MerchantStockEntry
{
    [SerializeField] private ItemData item;
    [SerializeField] private MerchantStockMode stockMode = MerchantStockMode.Infinite;
    [SerializeField] private int limitedAmount = 1;

    public ItemData Item => item;
    public MerchantStockMode StockMode => stockMode;
    public int LimitedAmount => Mathf.Max(0, limitedAmount);
    public bool IsInfinite => stockMode == MerchantStockMode.Infinite;
}