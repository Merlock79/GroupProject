using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CubeExplosion : MonoBehaviour
{
    public const string DestructibleTag = "Destructible";

    [Header("Вибух")]
    [SerializeField, Min(0.1f)] private float radius = 4f;
    [SerializeField] private LayerMask affectedLayers = ~0;

    [Header("Кулдаун")]
    [SerializeField, Min(0f)] private float cooldown = 1.5f;

    [Header("Ефект")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField, Min(0.1f)] private float effectLifetime = 2f;
    [SerializeField] private bool scaleEffectToRadius = false;
    [SerializeField, Min(0.01f)] private float effectBaseRadius = 2f;

    [Header("Material Flash")]
    [SerializeField, Min(0.01f)] private float flashDuration = 0.4f;
    [SerializeField] private Color flashColor = new Color(1f, 0.95f, 0.6f);
    [SerializeField] private Color flashOrangeColor = new Color(1f, 0.35f, 0.05f);

    private Material cubeMaterial;
    private Color initialColor;
    private Coroutine flashCoroutine;
    private float nextAllowedTime;

    public float Radius => radius;
    public float CooldownRemaining => Mathf.Max(0f, nextAllowedTime - Time.time);
    public bool IsReady => Time.time >= nextAllowedTime;

    private void Awake()
    {
        cubeMaterial = GetComponent<MeshRenderer>().material;
        initialColor = cubeMaterial.color;
    }

    private void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        cubeMaterial.color = initialColor;
    }

    private void OnDestroy()
    {
        Destroy(cubeMaterial);
    }

    public bool Explode()
    {
        if (!IsReady) return false;
        nextAllowedTime = Time.time + cooldown;

        Collider[] hits = Physics.OverlapSphere(
            transform.position, radius, affectedLayers, QueryTriggerInteraction.Ignore);

        var blocks = new HashSet<DestructibleBlock>();
        int destructibleCount = 0;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag(DestructibleTag)) continue;
            destructibleCount++;

            DestructibleBlock block = hit.GetComponentInParent<DestructibleBlock>();
            if (block != null) blocks.Add(block);
        }

        if (destructibleCount > 0)
            Debug.Log($"Вибух: знайдено Destructible: {destructibleCount} (блоків: {blocks.Count})");
        else
            Debug.Log("Вибух: Destructible у радіусі немає");

        foreach (DestructibleBlock block in blocks)
        {
            block.DestroyBlock();
        }

        SpawnEffect();
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(PlayMaterialFlash());
        return true;
    }

    private IEnumerator PlayMaterialFlash()
    {
        Color startColor = flashColor;
        Color orangeColor = flashOrangeColor;
        startColor.a = initialColor.a;
        orangeColor.a = initialColor.a;

        float elapsedTime = 0f;
        while (elapsedTime < flashDuration)
        {
            float progress = elapsedTime / flashDuration;
            cubeMaterial.color = progress < 0.25f
                ? Color.Lerp(startColor, orangeColor, progress / 0.25f)
                : Color.Lerp(orangeColor, initialColor,
                    Mathf.SmoothStep(0f, 1f, (progress - 0.25f) / 0.75f));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cubeMaterial.color = initialColor;
        flashCoroutine = null;
    }

    private void SpawnEffect()
    {
        if (effectPrefab == null) return;

        GameObject fx = Instantiate(effectPrefab, transform.position, Quaternion.identity);

        if (scaleEffectToRadius)
            fx.transform.localScale *= radius / effectBaseRadius;

        Destroy(fx, effectLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
