using UnityEngine;
using UnityEngine.Events;

public class Generator : MonoBehaviour
{
    [Header("Настройки прогресса")]
    public float maxProgress = 100f;
    [HideInInspector] public float currentProgress = 0f;

    [Tooltip("Сколько секунд займёт полная починка, если держать кнопку без перерыва (без учёта штрафов за провал скиллчека)")]
    public float repairTimeSeconds = 10f;

    private float repairSpeed => maxProgress / Mathf.Max(repairTimeSeconds, 0.01f);

    [Header("Скиллчеки")]
    public float minTimeBetweenChecks = 2f;
    public float maxTimeBetweenChecks = 5f;
    public float greatSuccessBonus = 8f;
    public float successBonus = 3f;
    public float failPenalty = 10f;

    [Header("Ссылки")]
    [Tooltip("Объект с UI-подсказкой над генератором (World Space Canvas), необязательно")]
    public GameObject interactionPrompt;

    [Header("События")]
    public UnityEvent onGeneratorRepaired;
    public UnityEvent<float> onProgressChanged;

    private bool isPlayerRepairing = false;
    private float nextSkillCheckTimer;
    private bool skillCheckActive = false;
    private bool isRepaired = false;

    void Update()
    {
        if (isRepaired || !isPlayerRepairing) return;

        currentProgress += repairSpeed * Time.deltaTime;
        currentProgress = Mathf.Clamp(currentProgress, 0, maxProgress);
        onProgressChanged?.Invoke(currentProgress / maxProgress);
        if (RepairHUD.Instance != null) RepairHUD.Instance.SetProgress(currentProgress / maxProgress);

        if (!skillCheckActive)
        {
            nextSkillCheckTimer -= Time.deltaTime;
            if (nextSkillCheckTimer <= 0f)
            {
                TriggerSkillCheck();
            }
        }

        if (currentProgress >= maxProgress)
        {
            CompleteRepair();
        }
    }

    public void StartRepair()
    {
        if (isRepaired) return;
        isPlayerRepairing = true;
        ResetSkillCheckTimer();

        if (RepairHUD.Instance != null)
        {
            RepairHUD.Instance.Show();
            RepairHUD.Instance.SetProgress(currentProgress / maxProgress);
        }
    }

    public void StopRepair()
    {
        isPlayerRepairing = false;

        if (skillCheckActive)
        {
            SkillCheckUI.Instance.CancelSkillCheck();
            skillCheckActive = false;
        }

        if (RepairHUD.Instance != null)
        {
            RepairHUD.Instance.Hide();
        }
    }

    public void ShowPrompt()
    {
        if (isRepaired) return;
        if (interactionPrompt != null) interactionPrompt.SetActive(true);
    }

    public void HidePrompt()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    void ResetSkillCheckTimer()
    {
        nextSkillCheckTimer = Random.Range(minTimeBetweenChecks, maxTimeBetweenChecks);
    }

    void TriggerSkillCheck()
    {
        skillCheckActive = true;
        SkillCheckUI.Instance.StartSkillCheck(OnSkillCheckResult);
    }

    void OnSkillCheckResult(SkillCheckResult result)
    {
        skillCheckActive = false;
        ResetSkillCheckTimer();

        switch (result)
        {
            case SkillCheckResult.Great:
                currentProgress += greatSuccessBonus;
                break;
            case SkillCheckResult.Success:
                currentProgress += successBonus;
                break;
            case SkillCheckResult.Fail:
                currentProgress -= failPenalty;
                break;
        }

        currentProgress = Mathf.Clamp(currentProgress, 0, maxProgress);
        onProgressChanged?.Invoke(currentProgress / maxProgress);
        if (RepairHUD.Instance != null) RepairHUD.Instance.SetProgress(currentProgress / maxProgress);

        if (currentProgress >= maxProgress)
        {
            CompleteRepair();
        }
    }

    void CompleteRepair()
    {
        isRepaired = true;
        isPlayerRepairing = false;
        if (RepairHUD.Instance != null) RepairHUD.Instance.Hide();
        HidePrompt();
        onGeneratorRepaired?.Invoke();
    }
}