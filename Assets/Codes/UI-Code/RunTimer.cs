using UnityEngine;
using TMPro;

public class RunTimer : MonoBehaviour
{
    public static RunTimer Instance; // lets other scripts use RunTimer.Instance

    [Header("UI")]
    public TMP_Text timerText;      // the main timer text
    public TMP_Text penaltyText;    // second text next to the timer, shows +3s when a hazard hits
    public float penaltyShowTime = 1.5f; // how long the +3s stays on screen (seconds)

    public float Time_ { get; private set; }         // seconds on the clock
    public float PenaltyTotal { get; private set; }  // total penalty seconds added so far

    private float penaltyShownAt = -10f; // when the last penalty appeared
    private bool stopped;                // true after the finish line
    private bool started;                // true after the player first moves

    void Awake() { Instance = this; }

    public static string FormatTime(float seconds, bool showHundredths = true) // turns seconds into 0:06:15.60
    {
        if (seconds < 0f) seconds = 0f;
        int total = Mathf.FloorToInt(seconds * 100f); // everything in hundredths of a second
        int hundredths = total % 100;
        int secs = (total / 100) % 60;
        int mins = (total / 6000) % 60;
        int hours = total / 360000;

        string text = hours.ToString() + ":" + mins.ToString("00") + ":" + secs.ToString("00");
        if (showHundredths) text += "." + hundredths.ToString("00");
        return text;
    }

    void Start()
    {
        if (penaltyText != null) penaltyText.text = ""; // hide the penalty text at the start
    }

    void Update()
    {
        if (started && !stopped && !LoadingScreen.IsLoading) // only count after the first move, never during loading
            Time_ += Time.deltaTime;

        if (timerText != null)
            timerText.text = "TIME " + FormatTime(Time_);

        if (penaltyText != null) // fade the +3s out after it appears
        {
            float age = Time.time - penaltyShownAt;
            if (age < penaltyShowTime)
            {
                Color c = penaltyText.color;
                c.a = 1f - (age / penaltyShowTime);
                penaltyText.color = c;
            }
            else
            {
                penaltyText.text = "";
            }
        }
    }

    public bool HasStarted { get { return started; } } // other scripts (like HeatSystem) check this

    public void StartTimer() // called by StartTimerOnMove on the first move
    {
        started = true;
    }

    public void Stop() // used by the finish line, freezes the clock
    {
        stopped = true;
    }

    public void AddPenalty(float seconds) // used by hazards, adds seconds as a penalty
    {
        Time_ += seconds;
        PenaltyTotal += seconds;

        if (penaltyText != null)
        {
            penaltyText.text = "+" + seconds.ToString("F0") + "s";
            penaltyText.color = Color.red;
            penaltyShownAt = Time.time;
        }
    }
}