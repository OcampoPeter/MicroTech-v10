using UnityEngine; // Unity basics
using UnityEngine.SceneManagement; // lets us load scenes

public class MainMenu : MonoBehaviour // the main menu buttons
{
    [Header("Exact name of the TS Area scene (must be in Build Profiles)")] // Inspector title
    public string tsSceneName = "TS Room"; // the name of the scene to load

    public void PlayGame() // called by the Play button
    {
        Time.timeScale = 1f; // make sure the game is not paused
        SceneManager.LoadScene(tsSceneName); // load the game scene
    }

    public void QuitGame() // called by the Quit button
    {
        Application.Quit(); // close the game (only works in a build)
    }
}