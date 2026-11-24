using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Complete,
    Crashed
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Screen (leave it turned OFF in the Hierarchy)")]
    public GameObject crashPanel;

    public GameState State = GameState.Playing;

    [Header("Where to go if the maze was opened on its own")]
    public string fallbackReturnScene = "TS Room";

    void Awake() { Instance = this; }

    void Start()
    {
        Time.timeScale = 1f;
        if (crashPanel != null) crashPanel.SetActive(false);
    }

    void Update()
    {
        if (State == GameState.Crashed && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    public void SetState(GameState newState)
    {
        if (State == newState) return;
        State = newState;
        Debug.Log("Game state is now: " + State.ToString());

        if (State == GameState.Crashed)
        {
            if (crashPanel != null) crashPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else if (State == GameState.Complete)
        {
            ReturnToTS();
        }
    }

    public void ReturnToTS()
    {
        if (State == GameState.Complete)
            PartRepair.MarkCurrentFixed();

        Time.timeScale = 1f;

        if (MazeLoader.Instance != null && MazeLoader.Instance.InMaze)
            MazeLoader.Instance.ReturnToTS();
        else
            SceneManager.LoadScene(string.IsNullOrEmpty(MapPortal.ReturnScene) ? fallbackReturnScene : MapPortal.ReturnScene);
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        if (MazeLoader.Instance != null && MazeLoader.Instance.InMaze)
            MazeLoader.Instance.RestartMaze();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}