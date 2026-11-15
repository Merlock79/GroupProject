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

    /// <summary>Закриває застосунок (у редакторі зупиняє Play Mode).</summary>
    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
