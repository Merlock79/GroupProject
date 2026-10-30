using UnityEngine;

[RequireComponent(typeof(TMPro.TextMeshProUGUI))]
public class CoinCounter : MonoBehaviour
{
    private TMPro.TextMeshProUGUI label;

    private void Awake()
    {
        label = GetComponent<TMPro.TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        CoinStorage.CoinsChanged += UpdateCount;
        UpdateCount(CoinStorage.GetCoins());
    }

    private void OnDisable()
    {
        CoinStorage.CoinsChanged -= UpdateCount;
    }

    private void UpdateCount(int total)
    {
        label.text = total.ToString();
    }
}
