using UnityEngine;
using UnityEngine.Serialization;

public class MouseLookOld : MonoBehaviour
{
    [FormerlySerializedAs("mouseSensitivity")]
    public float sensitivity = 150f;

    [FormerlySerializedAs("player")]
    public Transform playerBody;

    private float xRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
