using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraLocker : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private CinemachineCamera cam;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }

    private void Start()
    {
        TryBind();
    }

    private void Update()
    {
        if (cam.Target.TrackingTarget == null)
        {
            TryBind();
        }
        else
        {
            enabled = false;
        }
    }

    private void TryBind()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            cam.Target.TrackingTarget = player.transform;
        }
    }
}