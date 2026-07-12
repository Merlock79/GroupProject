using UnityEngine;

public class CloningFlower : MagicFlower
{
    public GameObject flowerPrefab;

    public override void Water()
    {
        Vector3 spawnPos = transform.position + new Vector3(1f, 0f, 0f);
        Instantiate(flowerPrefab, spawnPos, Quaternion.identity);
        Debug.Log("Квітка створила копію!");
    }
}
