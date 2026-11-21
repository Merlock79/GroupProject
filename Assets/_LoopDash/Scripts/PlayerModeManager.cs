using System;
using UnityEngine;

public enum PlayerMode { Cube, Ship }

public class PlayerModeManager : MonoBehaviour
{
    public static PlayerModeManager Instance { get; private set; }
    public event Action<GameObject> OnPlayerChanged;

    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject shipPrefab;
    [SerializeField] private Transform startPoint;
    [SerializeField] private PlayerMode startMode = PlayerMode.Cube;
    [SerializeField] private bool debugModeSwitching = true;

    public PlayerMode CurrentMode { get; private set; }
    public GameObject CurrentPlayer { get; private set; }
    private int lastSwapFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GameObject existing = GameObject.FindGameObjectWithTag("Player");
        Vector3 position = startPoint != null ? startPoint.position : existing != null ? existing.transform.position : transform.position;
        Quaternion rotation = existing != null ? existing.transform.rotation : startPoint != null ? startPoint.rotation : Quaternion.identity;
        CurrentMode = startMode;
        GameObject prefab = GetPrefab(startMode);
        if (prefab == null)
        {
            Debug.LogError($"PlayerModeManager: {startMode} prefab is not assigned", this);
            return;
        }
        CurrentPlayer = Instantiate(prefab, position, rotation);
        if (existing != null)
        {
            existing.SetActive(false);
            Destroy(existing);
        }
        ValidatePlayer(CurrentPlayer);
        OnPlayerChanged?.Invoke(CurrentPlayer);
    }

    private void Update()
    {
        if (!debugModeSwitching) return;
        if (Input.GetKeyDown(KeyCode.F1)) SetMode(PlayerMode.Cube);
        if (Input.GetKeyDown(KeyCode.F2)) SetMode(PlayerMode.Ship);
    }

    public void SetMode(PlayerMode mode)
    {
        if (mode == CurrentMode || lastSwapFrame == Time.frameCount || CurrentPlayer == null) return;
        GameObject prefab = GetPrefab(mode);
        if (prefab == null) return;

        lastSwapFrame = Time.frameCount;
        Vector3 position = CurrentPlayer.transform.position;
        Quaternion rotation = CurrentPlayer.transform.rotation;
        GameObject oldPlayer = CurrentPlayer;
        CurrentPlayer = Instantiate(prefab, position, rotation);
        CurrentMode = mode;
        oldPlayer.SetActive(false);
        Destroy(oldPlayer);
        ValidatePlayer(CurrentPlayer);
        OnPlayerChanged?.Invoke(CurrentPlayer);
    }

    private GameObject GetPrefab(PlayerMode mode) => mode == PlayerMode.Cube ? cubePrefab : shipPrefab;

    private void ValidatePlayer(GameObject player)
    {
        if (!player.CompareTag("Player"))
            Debug.LogError($"{player.name} prefab must use the Player tag", player);
        if (player.GetComponent<HazardDetector>() == null)
            Debug.LogError($"{player.name} prefab needs a HazardDetector", player);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
