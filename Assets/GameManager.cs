using UnityEngine; // Unity basics
using UnityEngine.InputSystem; // lets us read the keyboard
using UnityEngine.SceneManagement; // lets us load scenes

public enum GameState // the three states of the game
{
    Playing, // the game is running
    Complete, // the player finished
    Crashed // the game crashed
}

public class GameManager : MonoBehaviour // controls the end screens and restart
{
    public static GameManager Instance; // easy access from other scripts

    [Header("Screens (leave both turned OFF in the Hierarchy)")] // Inspector title
    public GameObject crashPanel; // the blue screen
    public GameObject completePanel; // the finish screen

    public GameState State = GameState.Playing; // the current state, visible in the Inspector

    void Awake() { Instance = this; } // save this script for easy access

    void Start() // runs once at the start
    {
        Time.timeScale = 1f; // make sure time is running
        if (crashPanel != null) crashPanel.SetActive(false); // hide the crash screen
        if (completePanel != null) completePanel.SetActive(false); // hide the finish screen
    }

    void Update() // runs every frame
    {
        if (State == GameState.Crashed && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) // crashed and R was pressed
            Restart(); // restart the maze
    }

    public void SetState(GameState newState) // changes the game state
    {
        if (State == newState) return; // already in this state, do nothing
        State = newState; // save the new state
        Debug.Log("Game state is now: " + State.ToString()); // show the state in the Console

        if (State == GameState.Crashed) // the game crashed
        {
            if (crashPanel != null) crashPanel.SetActive(true); // show the crash screen
            Time.timeScale = 0f; // freeze the game
        }
        else if (State == GameState.Complete) // the player finished
        {
            if (completePanel != null) completePanel.SetActive(true); // show the finish screen
            Time.timeScale = 0f; // freeze the game
        }
    }

    [Header("Where Return goes if no scene was remembered")] // Inspector title
    public string fallbackReturnScene = "TS Room"; // the scene to go back to if none was saved

    public void ReturnToTS() // called by the Return button
    {
        if (State == GameState.Complete) // only if the player finished
            PartRepair.MarkCurrentFixed(); // mark the part as fixed

        string scene = string.IsNullOrEmpty(MapPortal.ReturnScene) ? fallbackReturnScene : MapPortal.ReturnScene; // pick the scene to go back to
        Time.timeScale = 1f; // un-freeze the game
        SceneManager.LoadScene(scene); // load that scene
    }

    public void Restart() // restarts the current scene
    {
        Time.timeScale = 1f; // un-freeze the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // reload this scene
    }
}