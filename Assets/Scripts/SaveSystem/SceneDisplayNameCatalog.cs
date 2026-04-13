using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "SceneDisplayNameCatalog", menuName = "Game/Save System/Scene Display Name Catalog")]
public class SceneDisplayNameCatalog : ScriptableObject
{
    [Serializable]
    private class Entry
    {
        public string sceneName;
        public LocalizedString displayName;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<string, LocalizedString> cache;

    public bool TryGetDisplayName(string sceneName, out LocalizedString localizedString)
    {
        EnsureBuilt();
        return cache.TryGetValue(sceneName, out localizedString);
    }

    private void OnValidate()
    {
        cache = null;
    }

    private void EnsureBuilt()
    {
        if (cache != null)
            return;

        cache = new Dictionary<string, LocalizedString>();

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName))
                continue;

            if (!cache.ContainsKey(entry.sceneName))
                cache.Add(entry.sceneName, entry.displayName);
        }
    }
}