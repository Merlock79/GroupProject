using UnityEngine;

// ТИМЧАСОВО: замінити справжнім скриптом з EXPLO-02 (видалити цей файл).
public class DestructibleBlock : MonoBehaviour
{
    public void DestroyBlock()
    {
        Destroy(gameObject);
    }
}