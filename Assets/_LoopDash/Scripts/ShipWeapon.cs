using System;
using UnityEngine;

public sealed class ShipWeapon : MonoBehaviour
{
    [SerializeField] private ShipBullet bulletPrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(0f)] private float cooldown = 0.3f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField, Range(1, 5)] private int projectilesPerShot = 1;
    [SerializeField] private float[] shotAngles = { 0f, -15f, 15f, -30f, 30f };

    private float nextAllowedTime;

    public bool IsReady => isActiveAndEnabled && Time.time >= nextAllowedTime;
    public float CooldownRemaining => Mathf.Max(0f, nextAllowedTime - Time.time);
    public float CooldownProgress => cooldown <= 0f ? 1f : Mathf.Clamp01(1f - CooldownRemaining / cooldown);

    private void Awake()
    {
        if (projectilesPerShot < 1 || projectilesPerShot > 5 || shotAngles.Length < projectilesPerShot)
            throw new InvalidOperationException("ShipWeapon needs 1 to 5 projectiles and an angle for each projectile.");
    }

    public bool Shoot()
    {
        if (!IsReady)
            return false;

        nextAllowedTime = Time.time + cooldown;
        for (int i = 0; i < projectilesPerShot; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(shotAngles[i], Vector3.forward) * muzzle.right;
            ShipBullet bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            bullet.Initialize(direction, damage, transform);
        }

        return true;
    }
}
