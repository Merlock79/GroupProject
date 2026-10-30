using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private CoinAnimation coinAnimation;

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
        GetComponent<Collider>().enabled = false;
        CoinStorage.AddCoins(value);
        Debug.Log($"Монета собрана: +{value}, всего: {CoinStorage.GetCoins()}");

        StartCoroutine(FinishCollection());
    }

    private IEnumerator FinishCollection()
    {
        yield return coinAnimation.PlayCollectionAnimation();
        Destroy(gameObject);
    }
}
