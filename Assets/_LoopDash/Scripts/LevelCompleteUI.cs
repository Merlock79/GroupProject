using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the completion panel when the active level raises its completion event.</summary>
public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text attemptsLabel;
    [SerializeField] private Button retryButton;

    private Level completedLevel;

    private void OnEnable()
    {
        if (panel != null) panel.SetActive(false);
        Level.LevelCompleted += HandleLevelCompleted;
        if (retryButton != null) retryButton.onClick.AddListener(RetryLevel);
    }

    private void OnDisable()
    {
        Level.LevelCompleted -= HandleLevelCompleted;
        if (retryButton != null) retryButton.onClick.RemoveListener(RetryLevel);
    }

    private void HandleLevelCompleted(Level level)
    {
        completedLevel = level;
        if (attemptsLabel != null)
            attemptsLabel.text = $"Витрачено спроб: {level.Attempts}";
        if (panel != null) panel.SetActive(true);
    }

    private void RetryLevel()
    {
        if (completedLevel != null) completedLevel.RestartLevel();
    }
}
