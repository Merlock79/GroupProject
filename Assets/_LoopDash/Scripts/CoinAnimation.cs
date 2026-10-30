using System.Collections;
using UnityEngine;

public class CoinAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float rotationOffsetPerUnit = 30f;
    [SerializeField, Min(0f)] private float levitationHeight = 0.08f;
    [SerializeField, Min(0f)] private float levitationSpeed = 1f;
    [SerializeField, Min(0.01f)] private float collectionDuration = 0.2f;
    [SerializeField, Min(1f)] private float collectionPeakScale = 1.2f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private float rotationOffset;
    private float animationTime;

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        rotationOffset = transform.position.x * rotationOffsetPerUnit;
    }

    private void Update()
    {
        animationTime += Time.deltaTime;

        float heightOffset = Mathf.Sin(animationTime * levitationSpeed * 2f * Mathf.PI) * levitationHeight;
        transform.localPosition = initialPosition + Vector3.up * heightOffset;
        transform.localRotation = Quaternion.AngleAxis(Time.time * rotationSpeed + rotationOffset, Vector3.up) * initialRotation;
    }

    public IEnumerator PlayCollectionAnimation()
    {
        float elapsedTime = 0f;
        while (elapsedTime < collectionDuration)
        {
            float progress = elapsedTime / collectionDuration;
            float scale = progress < 0.25f
                ? Mathf.SmoothStep(1f, collectionPeakScale, progress / 0.25f)
                : Mathf.SmoothStep(collectionPeakScale, 0f, (progress - 0.25f) / 0.75f);

            transform.localScale = initialScale * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;
    }
}
