using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFirstPersonController : MonoBehaviour
{
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private float mouseSensitivity = 0.2f;
	[SerializeField] private Transform playerCamera;

	private float cameraPitch;
	private bool isCursorLocked = true;

	void Start()
	{
		LockCursor();
	}

	void Update()
	{
		HandleCursor();

		Move();

		if (isCursorLocked)
		{
			Look();
		}
	}

	private void HandleCursor()
	{
		if (Keyboard.current == null)
		{
			return;
		}

		if (Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			UnlockCursor();
		}

		if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
		{
			LockCursor();
		}
	}

	private void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		isCursorLocked = true;
	}

	private void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		isCursorLocked = false;
	}

	private void Move()
	{
		Vector2 input = Vector2.zero;

		if (Keyboard.current != null)
		{
			if (Keyboard.current.wKey.isPressed) input.y += 1;
			if (Keyboard.current.sKey.isPressed) input.y -= 1;
			if (Keyboard.current.dKey.isPressed) input.x += 1;
			if (Keyboard.current.aKey.isPressed) input.x -= 1;
		}

		Vector3 direction =
			transform.forward * input.y +
			transform.right * input.x;

		transform.position += direction.normalized * moveSpeed * Time.deltaTime;
	}

	private void Look()
	{
		if (Mouse.current == null)
		{
			return;
		}

		Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

		transform.Rotate(Vector3.up * mouseDelta.x);

		cameraPitch -= mouseDelta.y;
		cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

		playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
	}
}