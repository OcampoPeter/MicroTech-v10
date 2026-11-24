using UnityEngine; // Unity basics
using TMPro; // lets us use TextMeshPro

public class RunTimer : MonoBehaviour // the timer for the run
{
    public static RunTimer Instance; // easy access from other scripts

    [Header("UI")] // Inspector title
    public TMP_Text timerText; // the main timer text
    public TMP_Text penaltyText; // the text that shows +3s when a hazard hits
    public float penaltyShowTime = 1.5f; // how long the penalty text stays (seconds)

    public float Time_ { get; private set; } // seconds on the clock
    public float PenaltyTotal { get; private set; } // total penalty seconds so far

    private float penaltyShownAt = -10f; // when the last penalty appeared
    private bool stopped; // true after the finish line
    private bool started; // true after the first move

    void Awake() { Instance = this; } // save this script for easy access

    public static string FormatTime(float seconds, bool showHundredths = true) // turns seconds into 0:06:15.60
    {
        if (seconds < 0f) seconds = 0f; // never show negative time
        int total = Mathf.FloorToInt(seconds * 100f); // count everything in hundredths of a second
        int hundredths = total % 100; // the hundredths part
        int secs = (total / 100) % 60; // the seconds part
        int mins = (total / 6000) % 60; // the minutes part
        int hours = total / 360000; // the hours part

        string text = hours.ToString() + ":" + mins.ToString("00") + ":" + secs.ToString("00"); // join hours, minutes, seconds
        if (showHundredths) text += "." + hundredths.ToString("00"); // add the hundredths if wanted
        return text; // give back the finished text
    }

    void Start() // runs once at the start
    {
        if (penaltyText != null) penaltyText.text = ""; // hide the penalty text
    }

    void Update() // runs every frame
    {
        if (started && !stopped && !LoadingScreen.IsLoading) // timer is running and not loading
            Time_ += Time.deltaTime; // add the frame time to the clock

        if (timerText != null) // if there is a timer text
            timerText.text = "TIME " + FormatTime(Time_); // show the time

        if (penaltyText != null) // if there is a penalty text
        {
            float age = Time.time - penaltyShownAt; // how long since the penalty appeared
            if (age < penaltyShowTime) // still showing
            {
                Color c = penaltyText.color; // get the current color
                c.a = 1f - (age / penaltyShowTime); // fade it out over time
                penaltyText.color = c; // apply the color
            }
            else // time is up
            {
                penaltyText.text = ""; // clear the text
            }
        }
    }

    public bool HasStarted { get { return started; } } // other scripts can check if the timer started

    public void StartTimer() // called on the first move
    {
        started = true; // start the clock
    }

    public void Stop() // called at the finish line
    {
        stopped = true; // freeze the clock
    }

    public void AddPenalty(float seconds) // called by hazards
    {
        Time_ += seconds; // add the seconds to the clock
        PenaltyTotal += seconds; // add them to the penalty total

        if (penaltyText != null) // if there is a penalty text
        {
            penaltyText.text = "+" + seconds.ToString("F0") + "s"; // show how many seconds were added
            penaltyText.color = Color.red; // make it red
            penaltyShownAt = Time.time; // remember when it appeared
        }
    }
}