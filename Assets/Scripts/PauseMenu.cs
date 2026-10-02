using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Toggles the pause menu with ESC and freezes the battle while it is open.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private bool isPaused;

    private void Start()
    {
        isPaused = false;
        SetPausePanel(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // Don't allow pausing once the battle has ended
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        SetPausePanel(isPaused);
    }

    public void Resume()
    {
        isPaused = false;
        SetPausePanel(false);
    }

    public void Restart()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenu.MainMenuSceneName);
    }

    private void SetPausePanel(bool visible)
    {
        if (pausePanel != null) pausePanel.SetActive(visible);
        Time.timeScale = visible ? 0f : 1f;
    }
}