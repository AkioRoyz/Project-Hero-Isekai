using System;

[Serializable]
public class InventoryEntry
{
    public ItemData Item;
    public int Amount;

    public InventoryEntry(ItemData item, int amount)
    {
        Item = item;
        Amount = amount;
    }
}