using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPadTestVerifier : MonoBehaviour
{
    [SerializeField] private CubeMovement movement;
    [SerializeField, Min(1)] private int expectedLaunches = 3;

    private Rigidbody body;
    private readonly HashSet<JumpPad> encounteredPads = new HashSet<JumpPad>();
    private int resolvedLaunches;
    private int passedLaunches;
    private Vector3 previousPosition;
    private bool hasPreviousPosition;
    private bool horizontalSpeedFailed;

    public bool IsComplete { get; private set; }
    public int PassedLaunches => passedLaunches;
    public int FailedLaunches => resolvedLaunches - passedLaunches;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<CubeMovement>();
        body = GetComponent<Rigidbody>();
        previousPosition = body.position;
    }

    private void FixedUpdate()
    {
        Vector3 currentPosition = body.position;
        if (hasPreviousPosition && movement.IsAirborne)
        {
            float observedSpeed = Mathf.Abs(currentPosition.x - previousPosition.x) / Time.fixedDeltaTime;
            float tolerance = Mathf.Max(0.15f, movement.HorizontalSpeed * 0.05f);
            if (!horizontalSpeedFailed && Mathf.Abs(observedSpeed - movement.HorizontalSpeed) > tolerance)
            {
                horizontalSpeedFailed = true;
                Debug.LogError($"JumpPad test failed: horizontal speed dropped to {observedSpeed:0.##}.", this);
            }
        }

        previousPosition = currentPosition;
        hasPreviousPosition = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        JumpPad pad = other.GetComponentInParent<JumpPad>();
        if (pad == null || !encounteredPads.Add(pad)) return;

        StartCoroutine(VerifyLaunch(pad));
    }

    private IEnumerator VerifyLaunch(JumpPad pad)
    {
        // Let all trigger callbacks from this physics step finish first.
        yield return new WaitForFixedUpdate();

        bool launched = movement.IsAirborne &&
                        Mathf.Abs(movement.CurrentJumpHeight - pad.JumpHeight) < 0.001f;
        resolvedLaunches++;

        if (launched)
        {
            passedLaunches++;
            Debug.Log($"JumpPad test passed: {pad.name}, height {pad.JumpHeight:0.##}.", pad);
        }
        else
        {
            Debug.LogError($"JumpPad test failed: {pad.name} did not apply height {pad.JumpHeight:0.##}.", pad);
        }

        if (resolvedLaunches >= expectedLaunches)
        {
            IsComplete = true;
            if (passedLaunches != expectedLaunches)
            {
                Debug.LogError($"JumpPad test complete: {FailedLaunches} of {expectedLaunches} variants failed to launch.", this);
            }
            else if (horizontalSpeedFailed)
            {
                Debug.LogError("JumpPad test complete: all variants launched, but horizontal speed dropped during a jump.", this);
            }
            else
            {
                Debug.Log($"JumpPad test complete: all {passedLaunches} variants launched the player without input and kept horizontal speed.", this);
            }
        }
    }
}
