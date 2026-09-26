using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PlayerPhoneIK : MonoBehaviour
{
    private static readonly HumanBodyBones[] PickupTorsoBones =
    {
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.UpperChest
    };

    private static readonly float[] PickupTorsoBoneWeights = { 0.5f, 0.3f, 0.2f };

    private static readonly HumanBodyBones[] LeftFingerBones =
    {
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal
    };

    [Header("Targets")]
    [SerializeField] private Transform earTarget;
    [SerializeField] private Transform pocketTarget;
    [SerializeField] private Transform heldPhone;
    [SerializeField] private float ikBlendSpeed = 8f;

    [Header("Timing")]
    [SerializeField] private float pickupDuration = 0.35f;
    [SerializeField] private float phoneToEarDuration = 0.35f;
    [SerializeField] private float phoneCallDuration = 5f;
    [SerializeField] private float putAwayDuration = 0.35f;

    [Header("Pickup Hand Target")]
    [SerializeField, Tooltip("Offset in the scene phone's local space.")]
    private Vector3 pickupHandPositionOffset;
    [SerializeField, Tooltip("Independent angles around the scene phone's fixed X, Y, and Z axes.")]
    private Vector3 pickupHandRotationOffset;
    [SerializeField, Range(0f, 0.9f)]
    private float pickupRotationStartProgress = 0.5f;

    [Header("Pickup Torso")]
    [SerializeField, Range(-75f, 75f)] private float pickupTorsoBendAngle = 35f;
    [SerializeField, Min(0.01f)] private float pickupTorsoReturnDuration = 0.35f;

    [Header("Events")]
    [SerializeField] private UnityEvent onPhoneCallStarted;
    [SerializeField] private UnityEvent onPhoneCallFinished;

    private readonly Quaternion[] leftFingerOpenRotations = new Quaternion[LeftFingerBones.Length];
    private readonly bool[] hasLeftFingerBone = new bool[LeftFingerBones.Length];
    private readonly Transform[] pickupTorsoBoneTransforms = new Transform[PickupTorsoBones.Length];
    private Animator animator;
    private Transform leftHand;
    private Transform scenePhone;
    private Vector3 handMoveStartPosition;
    private Quaternion handMoveStartRotation;
    private Vector3 handMoveEndPosition;
    private Quaternion handMoveEndRotation;
    private Vector3 handMoveStartLocalPosition;
    private Quaternion handMoveStartLocalRotation;
    private Transform handMoveTarget;
    private Transform pickupPhoneTarget;
    private float handMoveStartTime;
    private float handMoveDuration;
    private float pickupTorsoBendWeight;
    private PhoneState phoneState;
    private float ikWeight;
    private Coroutine phoneRoutine;

    public bool IsPhoneCallActive => phoneState != PhoneState.None;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

        if (leftHand == null)
        {
            Debug.LogError("PlayerPhoneIK requires a Humanoid Animator with a left hand bone.", this);
            return;
        }

        CacheOpenLeftHandPose();
        CachePickupTorsoBones();

        if (heldPhone == null)
        {
            Debug.LogError("PlayerPhoneIK requires a held phone assigned in the Player prefab.", this);
            return;
        }

        if (heldPhone.parent != leftHand)
            Debug.LogError("The held phone must be a child of the Animator's left hand bone.", this);

        heldPhone.gameObject.SetActive(false);
    }

    public void PickUpPhone(Transform phoneTransform)
    {
        if (phoneTransform == null)
        {
            Debug.LogError("PlayerPhoneIK cannot pick up a missing scene phone transform.", this);
            return;
        }

        if (heldPhone == null || leftHand == null)
            return;

        StopCurrentPhoneRoutine();

        scenePhone = phoneTransform;
        heldPhone.gameObject.SetActive(false);
        BeginHandMoveToPickupPose(scenePhone, pickupDuration);
        phoneState = PhoneState.ReachingPhone;
        phoneRoutine = StartCoroutine(PickupPhoneRoutine());
    }

    public void PutPhoneInPocket()
    {
        if (phoneState == PhoneState.None || heldPhone == null)
            return;

        StopCurrentPhoneRoutine();

        if (!heldPhone.gameObject.activeSelf)
            ActivateHeldPhone();

        BeginPutAway();
    }

    public void TakePhoneFromPocketToEar()
    {
        if (heldPhone == null || leftHand == null)
            return;

        StopCurrentPhoneRoutine();
        heldPhone.gameObject.SetActive(true);
        phoneRoutine = StartCoroutine(PhoneToEarRoutine());
    }

    private IEnumerator PickupPhoneRoutine()
    {
        yield return new WaitForSeconds(pickupDuration);

        if (scenePhone == null)
            yield break;

        ActivateHeldPhone();
        yield return PhoneToEarRoutine();
    }

    private IEnumerator PhoneToEarRoutine()
    {
        BeginHandMove(earTarget, phoneToEarDuration);
        phoneState = PhoneState.MovingToEar;

        yield return new WaitForSeconds(phoneToEarDuration);

        phoneState = PhoneState.Holding;
        onPhoneCallStarted.Invoke();

        yield return new WaitForSeconds(phoneCallDuration);
        BeginPutAway();
    }

    private void BeginPutAway()
    {
        BeginHandMove(pocketTarget, putAwayDuration);
        phoneState = PhoneState.PuttingAway;
        phoneRoutine = StartCoroutine(PutPhoneAwayRoutine());
    }

    private IEnumerator PutPhoneAwayRoutine()
    {
        yield return new WaitForSeconds(putAwayDuration);

        heldPhone.gameObject.SetActive(false);
        phoneState = PhoneState.None;
        onPhoneCallFinished.Invoke();
        phoneRoutine = null;
    }

    private void ActivateHeldPhone()
    {
        if (scenePhone != null)
            scenePhone.gameObject.SetActive(false);

        heldPhone.gameObject.SetActive(true);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (leftHand == null)
            return;

        float targetWeight = phoneState == PhoneState.None ? 0f : 1f;
        ikWeight = Mathf.MoveTowards(ikWeight, targetWeight, ikBlendSpeed * Time.deltaTime);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);

        if (phoneState == PhoneState.None || ikWeight <= 0f)
        {
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            return;
        }

        ApplyOpenLeftHandPose();
        ApplyPickupTorsoBend();
        EvaluateHandMove(out Vector3 position, out Quaternion rotation);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, position);

        float rotationWeight = ikWeight;
        if (pickupPhoneTarget != null)
            rotationWeight *= GetHandRotationProgress(GetHandMoveProgress());

        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationWeight);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, rotation);
    }

    private void BeginHandMove(Vector3 destinationPosition, Quaternion destinationRotation, float duration)
    {
        if (phoneState == PhoneState.None)
        {
            handMoveStartPosition = leftHand.position;
            handMoveStartRotation = leftHand.rotation;
        }
        else
        {
            EvaluateHandMove(out handMoveStartPosition, out handMoveStartRotation);
        }

        handMoveEndPosition = destinationPosition;
        handMoveEndRotation = destinationRotation;
        handMoveStartLocalPosition = transform.InverseTransformPoint(handMoveStartPosition);
        handMoveStartLocalRotation = Quaternion.Inverse(transform.rotation) * handMoveStartRotation;
        handMoveTarget = null;
        pickupPhoneTarget = null;
        handMoveStartTime = Time.time;
        handMoveDuration = Mathf.Max(0.01f, duration);
    }

    private void BeginHandMove(Transform destination, float duration)
    {
        BeginHandMove(destination.position, destination.rotation, duration);
        handMoveTarget = destination;
    }

    private void BeginHandMoveToPickupPose(Transform phoneTarget, float duration)
    {
        GetPickupHandPose(phoneTarget, out Vector3 position, out Quaternion rotation);
        BeginHandMove(position, rotation, duration);
        pickupPhoneTarget = phoneTarget;
    }

    private void EvaluateHandMove(out Vector3 position, out Quaternion rotation)
    {
        float progress = GetHandMoveProgress();
        Vector3 destinationPosition = handMoveEndPosition;
        Quaternion destinationRotation = handMoveEndRotation;

        if (pickupPhoneTarget != null)
            GetPickupHandPose(pickupPhoneTarget, out destinationPosition, out destinationRotation);
        else if (handMoveTarget != null)
        {
            destinationPosition = handMoveTarget.position;
            destinationRotation = handMoveTarget.rotation;
        }

        Vector3 startPosition = transform.TransformPoint(handMoveStartLocalPosition);
        Quaternion startRotation = transform.rotation * handMoveStartLocalRotation;

        position = Vector3.Lerp(startPosition, destinationPosition, progress);
        rotation = pickupPhoneTarget == null
            ? Quaternion.Slerp(startRotation, destinationRotation, progress)
            : destinationRotation;
    }

    private float GetHandRotationProgress(float positionProgress)
    {
        if (pickupPhoneTarget == null)
            return positionProgress;

        float rotationProgress = Mathf.InverseLerp(pickupRotationStartProgress, 1f, positionProgress);
        return rotationProgress * rotationProgress * (3f - 2f * rotationProgress);
    }

    private float GetHandMoveProgress()
    {
        float progress = Mathf.Clamp01((Time.time - handMoveStartTime) / handMoveDuration);
        return progress * progress * (3f - 2f * progress);
    }

    private void GetPickupHandPose(Transform phoneTarget, out Vector3 position, out Quaternion rotation)
    {
        position = phoneTarget.TransformPoint(pickupHandPositionOffset);

        Quaternion rotateX = Quaternion.AngleAxis(pickupHandRotationOffset.x, phoneTarget.right);
        Quaternion rotateY = Quaternion.AngleAxis(pickupHandRotationOffset.y, phoneTarget.up);
        Quaternion rotateZ = Quaternion.AngleAxis(pickupHandRotationOffset.z, phoneTarget.forward);
        rotation = rotateZ * rotateY * rotateX * phoneTarget.rotation;
    }

    private void CacheOpenLeftHandPose()
    {
        for (int i = 0; i < LeftFingerBones.Length; i++)
        {
            Transform fingerBone = animator.GetBoneTransform(LeftFingerBones[i]);
            if (fingerBone == null)
                continue;

            leftFingerOpenRotations[i] = fingerBone.localRotation;
            hasLeftFingerBone[i] = true;
        }
    }

    private void CachePickupTorsoBones()
    {
        for (int i = 0; i < PickupTorsoBones.Length; i++)
            pickupTorsoBoneTransforms[i] = animator.GetBoneTransform(PickupTorsoBones[i]);
    }

    private void ApplyOpenLeftHandPose()
    {
        for (int i = 0; i < LeftFingerBones.Length; i++)
        {
            if (hasLeftFingerBone[i])
                animator.SetBoneLocalRotation(LeftFingerBones[i], leftFingerOpenRotations[i]);
        }
    }

    private void ApplyPickupTorsoBend()
    {
        if (phoneState == PhoneState.ReachingPhone)
        {
            pickupTorsoBendWeight = GetHandMoveProgress();
        }
        else
        {
            pickupTorsoBendWeight = Mathf.MoveTowards(
                pickupTorsoBendWeight,
                0f,
                Time.deltaTime / pickupTorsoReturnDuration);
        }

        if (pickupTorsoBendWeight <= 0f)
            return;

        float bendAngle = pickupTorsoBendAngle * pickupTorsoBendWeight;
        for (int i = 0; i < pickupTorsoBoneTransforms.Length; i++)
        {
            Transform torsoBone = pickupTorsoBoneTransforms[i];
            if (torsoBone == null)
                continue;

            Quaternion bend = Quaternion.AngleAxis(bendAngle * PickupTorsoBoneWeights[i], Vector3.right);
            animator.SetBoneLocalRotation(PickupTorsoBones[i], torsoBone.localRotation * bend);
        }
    }

    private void StopCurrentPhoneRoutine()
    {
        if (phoneRoutine == null)
            return;

        StopCoroutine(phoneRoutine);
        phoneRoutine = null;
    }

    private enum PhoneState
    {
        None,
        ReachingPhone,
        MovingToEar,
        Holding,
        PuttingAway
    }
}
