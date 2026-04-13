using System;

[Serializable]
public struct RewardItemData
{
    public ItemData Item;
    public int Amount;

    public RewardItemData(ItemData item, int amount)
    {
        Item = item;
        Amount = amount;
    }
}