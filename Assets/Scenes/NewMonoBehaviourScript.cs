using System;
using UnityEngine;

public class MoneyBank : MonoBehaviour
{
    public event Action<float> OnMoneyChanged;

    public float currentMoney = 0f;
    public float moneyPerSecond = 1f;

    void Update()
    {
        currentMoney += moneyPerSecond * Time.deltaTime;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void SpendMoney(float amount)
    {
        currentMoney -= amount;
        if (currentMoney < 0) currentMoney = 0;

        OnMoneyChanged?.Invoke(currentMoney);
    }
}