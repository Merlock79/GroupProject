using UnityEngine;

public abstract class MagicFlower : MonoBehaviour
{
    [SerializeField] protected string flowerName;

    public abstract void Water();
}
