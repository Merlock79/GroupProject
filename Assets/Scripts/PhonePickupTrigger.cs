using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PhonePickupTrigger : MonoBehaviour
{
    [SerializeField] private Transform phone;
    [SerializeField, Range(0f, 180f)] private float requiredFacingAngle = 30f;

    private bool hasPickedUp;
    private PlayerPhoneIK pendingPlayerPhoneIK;
    private PhonePickupMovementRestriction pendingMovementRestriction;
    private Transform pendingPlayerTransform;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPickedUp || !other.CompareTag("Player"))
            return;

        pendingPlayerPhoneIK = other.GetComponentInChildren<PlayerPhoneIK>();
        pendingMovementRestriction = other.GetComponent<PhonePickupMovementRestriction>();
        if (pendingPlayerPhoneIK == null || pendingMovementRestriction == null)
        {
            Debug.LogError("PhonePickupTrigger requires PlayerPhoneIK and PhonePickupMovementRestriction on the player.", other);
            ClearPendingPlayer();
            return;
        }

        pendingPlayerTransform = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (hasPickedUp || other.transform != pendingPlayerTransform)
            return;

        ClearPendingPlayer();
    }

    private void Update()
    {
        if (hasPickedUp || pendingPlayerTransform == null || !IsPlayerFacingPhone())
            return;

        hasPickedUp = true;
        pendingMovementRestriction.BeginRestriction();
        pendingPlayerPhoneIK.PickUpPhone(phone);
        ClearPendingPlayer();
    }

    private bool IsPlayerFacingPhone()
    {
        Vector3 directionToPhone = phone.position - pendingPlayerTransform.position;
        directionToPhone.y = 0f;

        if (directionToPhone.sqrMagnitude <= 0.001f)
            return true;

        Vector3 playerForward = pendingPlayerTransform.forward;
        playerForward.y = 0f;
        return Vector3.Angle(playerForward, directionToPhone) <= requiredFacingAngle;
    }

    private void ClearPendingPlayer()
    {
        pendingPlayerPhoneIK = null;
        pendingMovementRestriction = null;
        pendingPlayerTransform = null;
    }
}
