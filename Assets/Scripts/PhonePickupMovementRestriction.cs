using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PhonePickupMovementRestriction : MonoBehaviour
{
    private const float TransitionRatio = 0.2f;

    [Header("Slowdown")]
    [SerializeField, Range(0f, 1f)] private float restriction = 0.7f;
    [SerializeField, Min(0f)] private float slowdownDuration = 0.5f;

    private PlayerMovement playerMovement;
    private Coroutine restrictionRoutine;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnDisable()
    {
        playerMovement.SetMovementRestriction(0f);
    }

    public void BeginRestriction()
    {
        if (restrictionRoutine != null)
            StopCoroutine(restrictionRoutine);

        restrictionRoutine = StartCoroutine(RestrictMovement());
    }

    private IEnumerator RestrictMovement()
    {
        float transitionDuration = slowdownDuration * TransitionRatio;
        float restrictedDuration = Mathf.Max(0f, slowdownDuration - transitionDuration * 2f);
        yield return BlendRestriction(0f, restriction, transitionDuration);
        yield return new WaitForSeconds(restrictedDuration);
        yield return BlendRestriction(restriction, 0f, transitionDuration);
        restrictionRoutine = null;
    }

    private IEnumerator BlendRestriction(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            playerMovement.SetMovementRestriction(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            playerMovement.SetMovementRestriction(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        playerMovement.SetMovementRestriction(to);
    }
}
