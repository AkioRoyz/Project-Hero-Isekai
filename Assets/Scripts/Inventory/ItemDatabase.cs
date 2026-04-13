using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Save System/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> itemById;

    public IReadOnlyList<ItemData> Items => items;

    public bool TryGetItem(string itemId, out ItemData item)
    {
        EnsureBuilt();
        return itemById.TryGetValue(itemId, out item);
    }

    public ItemData GetItemOrNull(string itemId)
    {
        EnsureBuilt();

        ItemData item;
        return itemById.TryGetValue(itemId, out item) ? item : null;
    }

    public void RebuildCache()
    {
        itemById = null;
        EnsureBuilt();
    }

    private void OnValidate()
    {
        itemById = null;
    }

    private void EnsureBuilt()
    {
        if (itemById != null)
            return;

        itemById = new Dictionary<string, ItemData>();

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.ItemId))
            {
                Debug.LogWarning($"ItemDatabase: item '{item.name}' has empty ItemId.", item);
                continue;
            }

            if (itemById.ContainsKey(item.ItemId))
            {
                Debug.LogWarning($"ItemDatabase: duplicate ItemId '{item.ItemId}' found on '{item.name}'.", item);
                continue;
            }

            itemById.Add(item.ItemId, item);
        }
    }
}