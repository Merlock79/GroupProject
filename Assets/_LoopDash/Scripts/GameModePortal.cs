using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GameModePortal : MonoBehaviour
{
    [SerializeField] private PlayerMode targetMode;
    [SerializeField] private Renderer portalVisual;
    [SerializeField] private Color cubeColor = new Color(0.2f, 1f, 0.35f, 1f);
    [SerializeField] private Color shipColor = new Color(0.65f, 0.25f, 1f, 1f);

    public PlayerMode TargetMode { get => targetMode; set => targetMode = value; }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (portalVisual == null) portalVisual = GetComponentInChildren<Renderer>();
        if (portalVisual != null) portalVisual.material.color = targetMode == PlayerMode.Cube ? cubeColor : shipColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerModeManager manager = PlayerModeManager.Instance;
        if (manager == null || manager.CurrentPlayer == null || manager.CurrentMode == targetMode) return;
        if (other.transform.root.gameObject != manager.CurrentPlayer) return;
        manager.SetMode(targetMode);
    }
}
