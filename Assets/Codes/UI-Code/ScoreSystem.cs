using UnityEngine; // Unity basics
using TMPro; // lets us use TextMeshPro

public class ScoreSystem : MonoBehaviour // works out and shows the score
{
    public static ScoreSystem Instance; // easy access from other scripts

    [Header("UI")] // Inspector title
    public TMP_Text scoreText; // the text that shows the score

    [Header("Score formula")] // Inspector title
    public int baseScore = 1000; // the score you start with
    [Tooltip("The time you should beat on difficulty 1 (seconds).")] // hint in the Inspector
    public float parTime = 60f; // the target time on difficulty 1
    public int pointsPerSecondUnderPar = 10; // points gained for each second faster than par
    public int pointsLostPerFall = 150; // points lost for each fall
    [Tooltip("Points lost for every 1% of corruption you finish with.")] // hint in the Inspector
    public float pointsLostPerCorruptionPercent = 5f; // points lost per 1% corruption

    [Header("Difficulty scaling")] // Inspector title
    [Tooltip("Each difficulty level makes the par time this much shorter. 0.1 = 10% shorter.")] // hint in the Inspector
    public float parShrinkPerLevel = 0.1f; // how much shorter par gets per level
    [Tooltip("Each difficulty level adds this much to the score multiplier. 0.25 = +25%.")] // hint in the Inspector
    public float scoreBonusPerLevel = 0.25f; // how much bonus score per level

    [Tooltip("Only used when you press Play directly in this maze scene (no part carried in). 0 = use 1.")] // hint in the Inspector
    public int directPlayDifficulty = 0; // difficulty to use when testing this scene alone

    public int Difficulty { get; private set; } // the current difficulty level
    public float CurrentPar { get; private set; } // the par time for this difficulty

    public int Falls { get; private set; } // how many times the player fell
    public int Score { get; private set; } // the final score
    public int BestScore { get; private set; } // the best score saved
    public bool IsNewBest { get; private set; } // true if this run beat the best score

    private const string BestKey = "BestScore"; // the name the best score is saved under
    public bool IsFinished { get; private set; } // true after the run is finished

    void Awake() { Instance = this; } // save this script for easy access

    void Start() // runs once at the start
    {
        BestScore = PlayerPrefs.GetInt(BestKey, 0); // load the saved best score (0 if none)

        bool cameFromPart = !string.IsNullOrEmpty(PartRepair.CurrentPart); // true if a part was carried in
        int level = (!cameFromPart && directPlayDifficulty > 0) ? directPlayDifficulty : PartRepair.CurrentDifficulty; // pick which difficulty to use
        Difficulty = Mathf.Max(1, level); // difficulty is never below 1
        CurrentPar = parTime * Mathf.Pow(1f - parShrinkPerLevel, Difficulty - 1); // make par shorter for higher difficulty
        Debug.Log("Difficulty " + Difficulty.ToString() + ", par time " + CurrentPar.ToString("F1") + "s"); // show the result in the Console

        UpdateText(); // update the score text
    }

    public void RegisterFall() // call this when the player falls
    {
        if (IsFinished) return; // ignore falls after finishing
        Falls++; // add one fall
    }

    public void Finish() // call this when the player finishes
    {
        if (IsFinished) return; // do nothing if already finished
        IsFinished = true; // mark as finished
        if (GameManager.Instance != null) // if there is a Game Manager
            GameManager.Instance.SetState(GameState.Complete); // tell it the level is complete

        RunTimer timer = RunTimer.Instance; // get the timer
        timer.Stop(); // stop the clock

        int secondsUnderPar = Mathf.RoundToInt(CurrentPar - timer.Time_); // how many seconds faster than par
        float corruption = CorruptionSystem.Instance != null ? CorruptionSystem.Instance.Corruption : 0f; // get the corruption (0 if none)
        int corruptionPenalty = Mathf.RoundToInt(corruption * pointsLostPerCorruptionPercent); // points lost from corruption
        int rawScore = baseScore + secondsUnderPar * pointsPerSecondUnderPar // base score plus time bonus
                     - Falls * pointsLostPerFall - corruptionPenalty; // minus fall and corruption penalties
        if (rawScore < 0) rawScore = 0; // score never goes below zero

        float multiplier = 1f + scoreBonusPerLevel * (Difficulty - 1); // harder levels give a bigger multiplier
        Score = Mathf.RoundToInt(rawScore * multiplier); // the final score

        if (Score > BestScore) // beat the best score
        {
            BestScore = Score; // save it as the new best
            IsNewBest = true; // remember it is a new best
            PlayerPrefs.SetInt(BestKey, BestScore); // store the best score
            PlayerPrefs.Save(); // write it to disk
        }

        UpdateText(); // update the score text
    }

    void UpdateText() // updates the score text on screen
    {
        if (scoreText == null) return; // no text, do nothing
        string best = "BEST " + BestScore.ToString(); // the best score words
        if (!IsFinished) // still playing
        {
            scoreText.text = "SCORE ---   " + best; // show dashes and the best score
            return; // stop here
        }
        scoreText.text = "FINISHED! SCORE " + Score.ToString() + "\n" + (IsNewBest ? "NEW BEST!" : best); // show the final score
    }

    [ContextMenu("Reset Best Score")] // adds a right-click option in the Inspector
    void ResetBestScore() // wipes the saved best score
    {
        PlayerPrefs.DeleteKey(BestKey); // delete the saved score
        BestScore = 0; // set it back to 0
        UpdateText(); // update the text
    }
}