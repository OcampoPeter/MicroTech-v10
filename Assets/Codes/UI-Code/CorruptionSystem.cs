using UnityEngine; // Unity basics
using TMPro; // lets us use TextMeshPro

public class CorruptionSystem : MonoBehaviour // tracks how corrupted the game is
{
    public static CorruptionSystem Instance; // easy access from other scripts

    [Header("UI")] // Inspector title
    public TMP_Text corruptionText; // the text that shows corruption

    [Header("Settings")] // Inspector title
    public float maxCorruption = 100f; // corruption level that crashes the game

    public float Corruption { get; private set; } // the current corruption
    public bool IsCrashed { get; private set; } // true after the game crashed

    void Awake() { Instance = this; } // save this script for easy access

    void Start() { UpdateText(); } // show the starting text

    public void AddCorruption(float amount) // adds corruption
    {
        if (IsCrashed) return; // do nothing if already crashed

        Corruption = Mathf.Min(maxCorruption, Corruption + amount); // add it but never go over the max
        UpdateText(); // update the text

        if (Corruption >= maxCorruption) Crash(); // crash when it reaches the max
    }

    void Crash() // crashes the game
    {
        IsCrashed = true; // remember it crashed
        if (GameManager.Instance != null) // if there is a Game Manager
            GameManager.Instance.SetState(GameState.Crashed); // tell it to show the crash screen
    }

    void UpdateText() // updates the text on screen
    {
        if (corruptionText != null) // if there is a text
            corruptionText.text = "CORRUPTION " + Corruption.ToString("F0") + "%"; // show corruption as a percent
    }
}