using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    public static event Action<Level> LevelCompleted;
    [Header("Посилання")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform finishPoint;

    [Header("Перезапуск")]
    [SerializeField, Min(0f)] private float restartDelay = 1f;

    [Header("Прогрес")]
    [SerializeField] private bool logProgress = true;

    private static int deathCount;

    private HazardDetector hazardDetector;
    private InputAction returnToMenuAction;
    private bool deathHandled;
    private bool finished;

    private float startX;
    private int lastLoggedPercent = -1;

    public float Progress { get; private set; }
    public int Attempts => deathCount + 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        deathCount = 0;
        LevelCompleted = null;
    }

    private void Awake()
    {
        returnToMenuAction = new InputAction("Return to Menu", InputActionType.Button, "<Keyboard>/escape");
    }

    private void OnEnable()
    {
        if (player == null || finishPoint == null)
        {
            Debug.LogError("Level: призначте Player і FinishPoint в Inspector", this);
            enabled = false;
            return;
        }

        hazardDetector = player.GetComponent<HazardDetector>();
        if (hazardDetector == null)
        {
            Debug.LogError("Level: на Player немає HazardDetector", this);
            enabled = false;
            return;
        }

        startX = player.position.x;
        if (finishPoint.position.x <= startX)
        {
            Debug.LogError("Level: FinishPoint має бути правіше за старт гравця по X", this);
            enabled = false;
            return;
        }

        Progress = 0f;
        lastLoggedPercent = -1;
        hazardDetector.OnHazardDetected += HandleHazard;
        returnToMenuAction.performed += ReturnToMenu;
        returnToMenuAction.Enable();
    }

    private void OnDisable()
    {
        returnToMenuAction.Disable();
        returnToMenuAction.performed -= ReturnToMenu;
        if (hazardDetector != null)
            hazardDetector.OnHazardDetected -= HandleHazard;
    }

    private void OnDestroy()
    {
        returnToMenuAction.Dispose();
    }

    private void ReturnToMenu(InputAction.CallbackContext context)
    {
        StopAllCoroutines();
        deathCount = 0;
        SceneManager.LoadScene("MainMenu");
    }

    private void HandleHazard(GameObject hazard)
    {
        if (deathHandled || finished) return;
        deathHandled = true;

        deathCount++;
        Debug.Log($"Смерть №{deathCount} (від {hazard.name}), прогрес {Progress:F0}%. Перезапуск через {restartDelay} с");

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        ReloadCurrentScene();
    }

    public void RestartLevel()
    {
        deathCount = 0;
        ReloadCurrentScene();
    }

    private static void ReloadCurrentScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        // Test scenes are often opened directly in the editor and are not included
        // in Build Settings, so their buildIndex is -1. Reload them by asset path.
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                activeScene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        Debug.LogError($"Cannot reload scene '{activeScene.name}': it is not included in Build Settings.");
    }

    private void Update()
    {
        UpdateProgress();

        if (finished || deathHandled) return;

        if (player.position.x >= finishPoint.position.x)
        {
            finished = true;
            Debug.Log("Перемога! Рівень пройдено.");
            LevelCompleted?.Invoke(this);
        }
    }

    private void UpdateProgress()
    {
        float finishX = finishPoint.position.x;
        float t = Mathf.InverseLerp(startX, finishX, player.position.x);
        Progress = t * 100f;

        if (!logProgress) return;

        int percent = Mathf.FloorToInt(Progress);
        if (percent != lastLoggedPercent)
        {
            lastLoggedPercent = percent;
            Debug.Log($"Прогрес: {percent}%");
        }
    }

    private void OnDrawGizmos()
    {
        if (finishPoint == null) return;
        Gizmos.color = Color.green;
        Vector3 p = finishPoint.position;
        Gizmos.DrawLine(p + Vector3.down * 20f, p + Vector3.up * 20f);
    }
}
