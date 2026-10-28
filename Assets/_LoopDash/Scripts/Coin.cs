using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField] private string playerTag = "Player";

    private bool collected;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag)) return;

        collected = true;
        CoinStorage.AddCoins(value);
        Debug.Log($"Монета собрана: +{value}, всего: {CoinStorage.GetCoins()}");

        Destroy(gameObject);
    }
}