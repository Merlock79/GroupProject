using System;
using UnityEngine;

public sealed class DamageableBlock : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float maxHealth = 1f;

    public float MaxHealth => maxHealth;
    public float Health { get; private set; }

    private void Awake()
    {
        Health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (!float.IsFinite(damage) || damage < 0f)
            throw new ArgumentOutOfRangeException(nameof(damage));

        if (Health <= 0f)
            return;

        Health = Mathf.Max(0f, Health - damage);
        if (Health == 0f)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
