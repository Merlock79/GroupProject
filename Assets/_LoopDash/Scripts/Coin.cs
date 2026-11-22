using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [SerializeField, Min(1)] private float value = 1f;
    [SerializeField] private string playerTag = "Player";
    [FormerlySerializedAs("coinAnimation")]
    [SerializeField] private PickupAnimation pickupAnimation;
    [SerializeField] private UpgradeDefinition doubleCoinChanceUpgrade;

    private bool collected;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag)) return;

        float reward = CalculateReward();
        collected = true;
        GetComponent<Collider>().enabled = false;
        CoinStorage.AddCoins(reward);
        Debug.Log($"Coin collected: +{reward:0.##}, total: {CoinStorage.GetCoins():0.##}.");

        StartCoroutine(FinishCollection());
    }

    private float CalculateReward()
    {
        ProgressionManager manager = ProgressionManager.Instance;
        var doubleChance = (DoubleCoinChanceEffect)doubleCoinChanceUpgrade.Effect;
        return doubleChance.ShouldDouble(manager.GetActiveLevel(doubleCoinChanceUpgrade)) ? value * 2f : value;
    }

    private IEnumerator FinishCollection()
    {
        yield return pickupAnimation.PlayCollectionAnimation();
        Destroy(gameObject);
    }
}
