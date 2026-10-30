using System;
using UnityEngine;

public static class CoinStorage
{
    private const string Key = "TotalCoins";

    public static event Action<int> CoinsChanged;

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"CoinStorage: пропущено недопустимое значение {amount}");
            return;
        }

        long total = (long)GetCoins() + amount;
        if (total > int.MaxValue) total = int.MaxValue;

        PlayerPrefs.SetInt(Key, (int)total);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke((int)total);
    }

    public static int GetCoins()
    {
        return PlayerPrefs.GetInt(Key, 0);
    }

    public static void ResetCoins()
    {
        PlayerPrefs.SetInt(Key, 0);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(0);
    }
}
