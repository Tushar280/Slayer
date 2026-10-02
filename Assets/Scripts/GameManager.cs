using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the end-of-battle flow: detects who won, shows the win/lose panel
/// and offers restart / return to the main menu.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("End Game UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Text winText;
    [SerializeField] private Text loseText;

    [Header("References")]
    [SerializeField] private PlayerHealth player;
    [SerializeField] private EnemyHealth enemy;

    private const float EndGameTimeScale = 0.2f;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        Time.timeScale = 1f;
        Instance = this;

        SetPanelActive(winPanel, false);
        SetPanelActive(losePanel, false);

        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (enemy == null) enemy = FindAnyObjectByType<EnemyHealth>();
    }

    private void Update()
    {
        if (IsGameOver) return;

        // The enemy check comes first so a mutual kill counts as a win for the player
        if (enemy != null && enemy.currentHealth <= 0)
        {
            TriggerGameOver(playerWon: true);
        }
        else if (player != null && player.currentHealth <= 0)
        {
            TriggerGameOver(playerWon: false);
        }
    }

    /// <param name="playerWon">True if the player dragon defeated the AI dragon.</param>
    public void TriggerGameOver(bool playerWon)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (playerWon)
        {
            if (winText != null) winText.text = "VICTORY!\nThe AI Dragon has been slain.";
            SetPanelActive(winPanel, true);
        }
        else
        {
            if (loseText != null) loseText.text = "DEFEAT!\nYour Dragon has been slain.";
            SetPanelActive(losePanel, true);
        }

        Time.timeScale = EndGameTimeScale; // Dramatic slow-mo
    }

    public void RestartBattle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenu.MainMenuSceneName);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}