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

    private void OnEnable()
    {
        if (PlayerModeManager.Instance != null)
        {
            PlayerModeManager.Instance.OnPlayerChanged -= Bind;
            PlayerModeManager.Instance.OnPlayerChanged += Bind;
        }
    }

    private void Start()
    {
        if (PlayerModeManager.Instance != null)
        {
            PlayerModeManager.Instance.OnPlayerChanged -= Bind;
            PlayerModeManager.Instance.OnPlayerChanged += Bind;
            Bind(PlayerModeManager.Instance.CurrentPlayer);
        }
        else TryBind();
    }

    private void OnDisable()
    {
        if (PlayerModeManager.Instance != null)
            PlayerModeManager.Instance.OnPlayerChanged -= Bind;
    }

    private void Bind(GameObject player)
    {
        if (player != null) cam.Target.TrackingTarget = player.transform;
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
