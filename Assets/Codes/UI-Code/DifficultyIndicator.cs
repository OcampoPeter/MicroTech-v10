using UnityEngine; // Unity basics
using TMPro; // lets us use TextMeshPro

[RequireComponent(typeof(TMP_Text))] // this object must have a TextMeshPro text
public class DifficultyIndicator : MonoBehaviour // shows the difficulty level on screen
{
    [Header("Color for each level (level 1 = first color, level 5 = last)")] // Inspector title
    public Color[] levelColors = new Color[] // the list of colors
    {
        new Color(0.45f, 1.00f, 0.55f), // level 1 green
        new Color(0.80f, 1.00f, 0.35f), // level 2 lime
        new Color(1.00f, 0.90f, 0.30f), // level 3 yellow
        new Color(1.00f, 0.60f, 0.25f), // level 4 orange
        new Color(1.00f, 0.30f, 0.30f) // level 5 red
    };

    public int maxLevel = 5; // the highest difficulty level

    [Tooltip("Tick to also show the par time under the level.")] // hint in the Inspector
    public bool showPar = false; // show the par time or not

    private TMP_Text label; // the text we write to

    void Awake() // runs when the object loads
    {
        label = GetComponent<TMP_Text>(); // get the text on this object
    }

    void Update() // runs every frame
    {
        if (ScoreSystem.Instance == null) { label.text = ""; return; } // no score system, show nothing

        int level = ScoreSystem.Instance.Difficulty; // get the current level
        if (level < 1) { label.text = "LEVEL -"; return; } // level not ready yet

        string text = "LEVEL " + level.ToString() + "/" + maxLevel.ToString(); // build the level words
        if (showPar) // if we want to show par
            text += "\nPAR " + ScoreSystem.Instance.CurrentPar.ToString("F0") + "s"; // add the par time on a new line
        label.text = text; // show the words

        int index = Mathf.Clamp(level - 1, 0, levelColors.Length - 1); // pick a color number that is safe to use
        label.color = levelColors[index]; // color the text for this level
    }
}