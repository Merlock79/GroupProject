using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    [Header("Посилання")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform finishPoint;

    [Header("Перезапуск")]
    [SerializeField, Min(0f)] private float restartDelay = 1f;

    [Header("Прогрес")]
    [SerializeField] private bool logProgress = true;

    private static int deathCount;

    private HazardDetector hazardDetector;
    private bool deathHandled;
    private bool finished;

    private float startX;
    private int lastLoggedPercent = -1;

    public float Progress { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        deathCount = 0;
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
    }

    private void OnDisable()
    {
        if (hazardDetector != null)
            hazardDetector.OnHazardDetected -= HandleHazard;
    }

    private void HandleHazard(GameObject hazard)
    {
        if (deathHandled) return;
        deathHandled = true;

        deathCount++;
        Debug.Log($"Смерть №{deathCount} (від {hazard.name}), прогрес {Progress:F0}%. Перезапуск через {restartDelay} с");

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        UpdateProgress();

        if (finished || deathHandled) return;

        if (player.position.x >= finishPoint.position.x)
        {
            finished = true;
            Debug.Log("Перемога! Рівень пройдено.");
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