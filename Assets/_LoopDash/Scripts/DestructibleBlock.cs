using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    private bool destroyed;

    /// <summary>Разрушает блок. Вызывается только взрывом (CubeExplosion).</summary>
    public void DestroyBlock()
    {
        if (destroyed) return;
        destroyed = true;

        Destroy(gameObject);
    }
}