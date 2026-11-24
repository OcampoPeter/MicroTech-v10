using UnityEngine; // Unity basics
using TMPro; // lets us use TextMeshPro

public class ScoreMonitors : MonoBehaviour // shows the scores on the two TS room screens
{
    [Header("Monitor texts (drag them here)")] // Inspector title
    public TMP_Text playerScoreText; // Monitor 1: the score of each maze and the total
    public TMP_Text highScoreText; // Monitor 2: the high score

    [Header("Part ids, must match the Part Id of each PC part")] // Inspector title
    public string[] partIds = { "GPU", "RAM", "CPU" }; // the names of the parts, one per maze

    void OnEnable() // runs every time this object is switched on, including when we come back from a maze
    {
        Refresh(); // update both screens
    }

    public void Refresh() // updates both screens
    {
        RunScores rs = RunScores.Instance; // get the score keeper
        if (rs == null) return; // no score keeper, do nothing

        if (playerScoreText != null) // Monitor 1 is assigned
        {
            string text = ""; // the text we will build
            foreach (string id in partIds) // go through each part
                text += id + ":  " + rs.GetScore(id).ToString() + "\n"; // add one line with its score
            text += "TOTAL:  " + rs.Total.ToString(); // add the total at the end
            playerScoreText.text = text; // show it on Monitor 1
        }

        if (highScoreText != null) // Monitor 2 is assigned
            highScoreText.text = "HIGH SCORE\n" + rs.HighScore.ToString(); // show the high score on Monitor 2
    }
}