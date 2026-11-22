using System;
using UnityEngine;

public static class CoinStorage
{
    private const string LegacyKey = "TotalCoins";
    private const string BalanceKey = "TotalCoins.Float";

    public static event Action<float> CoinsChanged;

    public static void AddCoins(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
        {
            Debug.LogWarning($"CoinStorage ignored invalid coin amount {amount}.");
            return;
        }

        float total = Math.Min(GetCoins() + amount, float.MaxValue);
        PlayerPrefs.SetFloat(BalanceKey, total);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(total);
    }

    public static float GetCoins()
    {
        return PlayerPrefs.HasKey(BalanceKey)
            ? PlayerPrefs.GetFloat(BalanceKey)
            : PlayerPrefs.GetInt(LegacyKey, 0);
    }

    internal static bool CanAfford(int cost)
    {
        return CoversCost(GetCoins(), cost);
    }

    internal static bool TrySpendCoins(float amount, Action stagePurchase)
    {
        if (!float.IsFinite(amount) || amount < 0f)
            throw new ArgumentOutOfRangeException(nameof(amount));

        float balance = GetCoins();
        if (!CoversCost(balance, amount))
            return false;

        bool changesBalance = amount > 0f;
        bool hasFloatBalance = PlayerPrefs.HasKey(BalanceKey);
        float newBalance = Math.Max(0f, balance - amount);
        if (changesBalance)
            PlayerPrefs.SetFloat(BalanceKey, newBalance);

        try
        {
            stagePurchase();
        }
        catch (PlayerPrefsException)
        {
            if (changesBalance)
            {
                if (hasFloatBalance)
                    PlayerPrefs.SetFloat(BalanceKey, balance);
                else
                    PlayerPrefs.DeleteKey(BalanceKey);
            }

            throw;
        }

        // Flush both changes before notifying listeners that may read the new level.
        PlayerPrefs.Save();
        if (changesBalance)
            CoinsChanged?.Invoke(newBalance);

        return true;
    }

    private static bool CoversCost(float balance, float cost)
    {
        // Match the two-decimal balance shown in the UI when float arithmetic falls just below a cost.
        return balance >= cost || Math.Round(balance, 2) >= cost;
    }

    public static void ResetCoins()
    {
        PlayerPrefs.SetFloat(BalanceKey, 0f);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(0f);
    }
}
