using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Логіка головного меню Loop Runner.
/// Розмістити в Assets/Scripts/Menu/MainMenu.cs і повісити на об'єкт MainMenu у сцені.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Назва сцени тестового рівня (має бути додана до Build Settings).")]
    [SerializeField] private string testLevelSceneName = "TestLevel";
    [SerializeField] private CanvasGroup menuControls;
    [SerializeField] private ProgressionTreeUI progressionTree;
    [SerializeField] private UnityEngine.UI.Button progressionButton;

    private void OnEnable()
    {
        progressionTree.Closed += RestoreMenuControls;
    }

    private void OnDisable()
    {
        progressionTree.Closed -= RestoreMenuControls;
    }

    public void OpenProgression()
    {
        menuControls.interactable = false;
        progressionTree.gameObject.SetActive(true);
    }

    private void RestoreMenuControls()
    {
        menuControls.interactable = true;
        progressionButton.Select();
    }

    /// <summary>
    /// Тимчасово завантажує перший тестовий рівень.
    /// Надалі тут відкриватиметься панель вибору рівнів.
    /// </summary>
    public void Play()
    {
        if (!Application.CanStreamedLevelBeLoaded(testLevelSceneName))
        {
            Debug.LogError($"[MainMenu] Сцену '{testLevelSceneName}' не знайдено в Build Settings.");
            return;
        }

        SceneManager.LoadScene(testLevelSceneName);
    }

    /// <summary>Закриває застосунок.</summary>
    public void Exit()
    {
        Application.Quit();
    }
}
