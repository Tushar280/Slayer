using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the title screen buttons.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";
    private const string BattleSceneName = "Main Scene";

    public void PlayGame()
    {
        SceneManager.LoadScene(BattleSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}