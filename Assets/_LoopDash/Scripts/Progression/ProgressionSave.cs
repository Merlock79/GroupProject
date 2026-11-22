using System;
using System.Collections.Generic;
using UnityEngine;

internal static class ProgressionSave
{
    private const string Key = "LoopRunner.Progression";

    [Serializable]
    private sealed class SaveData
    {
        public List<LevelEntry> levels = new List<LevelEntry>();
    }

    [Serializable]
    private struct LevelEntry
    {
        public string id;
        public int level;
    }

    internal static Dictionary<string, int> Load()
    {
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        string json = PlayerPrefs.GetString(Key);
        if (string.IsNullOrEmpty(json))
            return levels;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"Could not read the progression save: {exception.Message}");
            return levels;
        }

        if (data?.levels != null)
        {
            foreach (LevelEntry entry in data.levels)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    levels[entry.id] = entry.level;
            }
        }

        return levels;
    }

    internal static void StagePurchase(
        IReadOnlyDictionary<UpgradeDefinition, int> levels, UpgradeDefinition purchasedUpgrade, int newLevel)
    {
        var data = new SaveData();
        foreach (KeyValuePair<UpgradeDefinition, int> entry in levels)
        {
            int level = entry.Key == purchasedUpgrade ? newLevel : entry.Value;
            if (level > 0)
                data.levels.Add(new LevelEntry { id = entry.Key.Id, level = level });
        }

        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
    }

    internal static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
