using System.Collections;
using UnityEngine;

public class JumpCrystalTestVerifier : MonoBehaviour
{
    [SerializeField] private CubeMovement movement;
    [SerializeField] private GameObject crystalPrefab;

    private int failures;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<CubeMovement>();
    }

    public void Configure(CubeMovement playerMovement, GameObject pickupPrefab)
    {
        movement = playerMovement;
        crystalPrefab = pickupPrefab;
    }

    private IEnumerator Start()
    {
        yield return WaitForPhysicsSteps(2);
        Check(!movement.HasExtraJump, "Player starts without a charge.");

        Vector3 playerPosition = movement.transform.position;
        GameObject firstCrystal = Instantiate(crystalPrefab, playerPosition, Quaternion.identity);
        GameObject secondCrystal = Instantiate(crystalPrefab, playerPosition, Quaternion.identity);
        Physics.SyncTransforms();
        yield return WaitForPhysicsSteps(4);

        Check(movement.HasExtraJump, "Overlapping crystals grant a charge.");
        Check(firstCrystal == null && secondCrystal == null,
            "Each crystal disappears, including one collected at capacity.");
        Check(!movement.TryAddExtraJump(), "A second charge is rejected while one is stored.");
        Check(IsAuraVisible(), "The aura appears while a charge is stored.");

        float normalJumpHeight = movement.CurrentJumpHeight;
        movement.SetJumpHeld(true);
        yield return WaitForPhysicsSteps(1);
        Check(movement.IsAirborne, "A new press launches the normal ground jump.");
        Check(movement.HasExtraJump, "The ground jump does not consume the charge.");

        movement.SetJumpHeld(false);
        yield return WaitForPhysicsSteps(1);
        movement.SetJumpHeld(true);
        yield return WaitForPhysicsSteps(1);
        Check(!movement.HasExtraJump, "A new press in the air consumes the charge.");
        Check(!IsAuraVisible(), "The aura disappears as soon as the charge is used.");
        Check(Mathf.Approximately(movement.CurrentJumpHeight, normalJumpHeight),
            "The extra jump uses the normal jump height.");

        GameObject nextCrystal = Instantiate(crystalPrefab, movement.transform.position, Quaternion.identity);
        Physics.SyncTransforms();
        yield return WaitForPhysicsSteps(3);
        Check(movement.HasExtraJump, "A new crystal can recharge the player during the same flight.");
        Check(IsAuraVisible(), "The aura returns with the new charge.");

        movement.SetJumpHeld(true);
        yield return WaitForPhysicsSteps(2);
        Check(movement.HasExtraJump, "Holding jump does not spend a newly collected charge.");
        Check(nextCrystal == null, "The second crystal is collected.");

        movement.SetJumpHeld(false);
        yield return WaitForPhysicsSteps(1);
        movement.SetJumpHeld(true);
        yield return WaitForPhysicsSteps(1);
        Check(!movement.HasExtraJump, "A later new press spends the recharged crystal.");
        Check(!IsAuraVisible(), "The aura clears after the recharged jump.");

        if (failures == 0)
            Debug.Log("JumpCrystal test passed: capacity, ground jump, two air jumps, mid-flight recharge, input edge, and aura.");
        else
            Debug.LogError($"JumpCrystal test failed: {failures} check(s) did not pass.");

        movement.enabled = false;
        Application.Quit(failures == 0 ? 0 : 1);
    }

    private IEnumerator WaitForPhysicsSteps(int count)
    {
        for (int i = 0; i < count; i++)
            yield return new WaitForFixedUpdate();
    }

    private bool IsAuraVisible()
    {
        Transform aura = movement.transform.Find("Visual/ExtraJumpAura");
        return aura != null && aura.gameObject.activeSelf;
    }

    private void Check(bool passed, string description)
    {
        if (passed)
        {
            Debug.Log($"JumpCrystal test passed: {description}");
            return;
        }

        failures++;
        Debug.LogError($"JumpCrystal test failed: {description}");
    }
}
