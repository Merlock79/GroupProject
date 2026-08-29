using UnityEngine;
using UnityEngine.UI;
using System;

public enum SkillCheckResult { Success, Great, Fail }

public class SkillCheckUI : MonoBehaviour
{
    public static SkillCheckUI Instance { get; private set; }

    [Header("Ссылки на UI")]
    public GameObject panel;
    public RectTransform needle;
    public Image successZoneImage;
    public Image greatZoneImage;

    [Header("Настройки")]
    public float rotationSpeed = 250f;
    public KeyCode checkKey = KeyCode.Space;
    [Range(5f, 90f)] public float successZoneWidth = 40f;
    [Range(2f, 30f)] public float greatZoneWidth = 12f;

    [Header("Калибровка (если текстуры зон нарисованы под разным углом)")]
    [Tooltip("Подкрути, если белая зона визуально не совпадает с реальным попаданием")]
    public float successZoneRotationOffset = 0f;
    [Tooltip("Подкрути, если зелёная зона визуально не совпадает с реальным попаданием")]
    public float greatZoneRotationOffset = 0f;

    private bool isActive = false;
    private float currentAngle = 0f;
    private float zoneCenterAngle;
    private Action<SkillCheckResult> onResult;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);

        SetupSimpleImage(successZoneImage);
        SetupSimpleImage(greatZoneImage);
    }

    void SetupSimpleImage(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Simple;
    }

    public void StartSkillCheck(Action<SkillCheckResult> callback)
    {
        onResult = callback;
        isActive = true;
        panel.SetActive(true);

        zoneCenterAngle = UnityEngine.Random.Range(0f, 360f);

        successZoneImage.rectTransform.localRotation =
            Quaternion.Euler(0, 0, -(zoneCenterAngle + successZoneRotationOffset));

        if (greatZoneImage != null)
        {
            greatZoneImage.rectTransform.localRotation =
                Quaternion.Euler(0, 0, -(zoneCenterAngle + greatZoneRotationOffset));
        }

        currentAngle = 0f;
        needle.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (!isActive) return;

        currentAngle += rotationSpeed * Time.deltaTime;
        needle.localRotation = Quaternion.Euler(0, 0, -currentAngle);

        if (currentAngle >= 360f)
        {
            Finish(SkillCheckResult.Fail);
            return;
        }

        if (Input.GetKeyDown(checkKey))
        {
            CheckHit();
        }
    }

    void CheckHit()
    {
        float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, zoneCenterAngle));

        // временный дебаг - помогает откалибровать offset'ы, потом можно удалить строку
        //Debug.Log($"[SkillCheck] currentAngle={currentAngle:F1} zoneCenter={zoneCenterAngle:F1} diff={diff:F1}");

        if (greatZoneImage != null && diff <= greatZoneWidth / 2f)
            Finish(SkillCheckResult.Great);
        else if (diff <= successZoneWidth / 2f)
            Finish(SkillCheckResult.Success);
        else
            Finish(SkillCheckResult.Fail);
    }

    public void CancelSkillCheck()
    {
        isActive = false;
        if (panel != null) panel.SetActive(false);
    }

    void Finish(SkillCheckResult result)
    {
        isActive = false;
        panel.SetActive(false);
        onResult?.Invoke(result);
    }
}