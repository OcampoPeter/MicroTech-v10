using UnityEngine; // Unity basics
using UnityEngine.UI; // lets us use the old UI Text
using UnityEngine.Events; // lets us use UnityEvent
using TMPro; // lets us use TextMeshPro

public enum ObjectiveType { Bent, Missing, Fried } // the 3 kinds of tasks

public class ObjectivesUI : MonoBehaviour // shows the task list on screen
{
    public static ObjectivesUI Instance; // easy access from other scripts

    [Header("Text rows (auto-found by name if left empty)")] // Inspector title
    public GameObject titleText; // the title row
    public GameObject bentText; // the Bent row
    public GameObject missingText; // the Missing row
    public GameObject friedText; // the Fried row

    [Header("Find the Exit line (made automatically if empty)")] // Inspector title
    public GameObject exitText; // the "Find the Exit" row
    public string exitMessage = "Find the Exit"; // the words shown at the end
    public Color exitColor = new Color(1f, 0.9f, 0.3f); // color of the exit message

    [Header("Panel (leave EMPTY if this script is ON the panel)")] // Inspector title
    public GameObject panel; // the whole panel

    [Header("Look")] // Inspector title
    public string title = "OBJECTIVES"; // the title words
    public Color pendingColor = Color.white; // color for tasks not done
    public Color doneColor = new Color(0.2f, 1f, 0.4f); // color for tasks done

    [Header("Event")] // Inspector title
    public UnityEvent onAllComplete; // runs when everything is fixed

    int[] total = new int[3]; // how many tasks of each type
    int[] fixedCount = new int[3]; // how many are fixed of each type
    bool allDone; // true once everything is fixed

    void Awake() // runs when the object loads
    {
        Instance = this; // save this script for easy access

        if (titleText == null) titleText = FindChildByName("title"); // find the title row by name
        if (bentText == null) bentText = FindChildByName("Bent"); // find the Bent row by name
        if (missingText == null) missingText = FindChildByName("Missing"); // find the Missing row by name
        if (friedText == null) friedText = FindChildByName("Fried"); // find the Fried row by name

        if (exitText == null && bentText != null) // no exit row yet
        {
            exitText = Instantiate(bentText, bentText.transform.parent); // copy the Bent row
            exitText.name = "ExitRow"; // name the copy
        }
        if (exitText != null) exitText.SetActive(false); // hide the exit row at the start
    }

    GameObject FindChildByName(string objName) // finds a child object by its name
    {
        Transform[] all = GetComponentsInChildren<Transform>(true); // get all children
        foreach (Transform t in all) // go through each child
        {
            if (t == transform) continue; // skip this object itself
            if (t.gameObject.name.ToLower() == objName.ToLower()) return t.gameObject; // return it if the name matches
        }
        return null; // nothing found
    }

    void SetLabel(GameObject obj, string message, Color color) // sets the words and color of a row
    {
        if (obj == null) return; // no object, do nothing

        TMP_Text tmp = obj.GetComponent<TMP_Text>(); // try to get a TextMeshPro text
        if (tmp != null) // it is TextMeshPro
        {
            tmp.text = message; // set the words
            tmp.color = color; // set the color
            return; // done
        }

        Text legacy = obj.GetComponent<Text>(); // try to get an old UI Text
        if (legacy != null) // it is an old Text
        {
            legacy.text = message; // set the words
            legacy.color = color; // set the color
            return; // done
        }

        Debug.LogWarning("[ObjectivesUI] '" + obj.name + "' has no Text component!"); // warn that there is no text
    }

    public void SetTotals(int bent, int missing, int fried) // sets how many tasks there are
    {
        total[0] = bent; // total Bent tasks
        total[1] = missing; // total Missing tasks
        total[2] = fried; // total Fried tasks
        fixedCount = new int[3]; // reset the fixed counts
        allDone = false; // nothing is done yet
        Refresh(); // update the screen
    }

    public void ReportFixed(ObjectiveType type) // call this when one task is fixed
    {
        int i = (int)type; // turn the type into a number
        if (fixedCount[i] < total[i]) fixedCount[i]++; // add one fixed, but never over the total
        Refresh(); // update the screen
    }

    public void ReportUnfixed(ObjectiveType type) // call this if a task breaks again
    {
        int i = (int)type; // turn the type into a number
        if (fixedCount[i] > 0) fixedCount[i]--; // remove one fixed, but never below zero
        Refresh(); // update the screen
    }

    void Refresh() // updates everything shown on screen
    {
        int all = total[0] + total[1] + total[2]; // the total number of tasks

        bool complete = all > 0 && fixedCount[0] == total[0] // true if everything is fixed
                                && fixedCount[1] == total[1] // Missing tasks all fixed
                                && fixedCount[2] == total[2]; // Fried tasks all fixed

        if (titleText != null) // there is a title row
        {
            titleText.SetActive(true); // show it
            SetLabel(titleText, title, pendingColor); // set its words
        }

        if (complete) // everything is fixed
        {
            if (bentText != null) bentText.SetActive(false); // hide the Bent row
            if (missingText != null) missingText.SetActive(false); // hide the Missing row
            if (friedText != null) friedText.SetActive(false); // hide the Fried row

            if (exitText != null) // there is an exit row
            {
                exitText.SetActive(true); // show it
                SetLabel(exitText, exitMessage, exitColor); // set its words and color
            }
        }
        else // not everything is fixed
        {
            if (exitText != null) exitText.SetActive(false); // hide the exit row

            SetRow(bentText, "Bent", 0); // update the Bent row
            SetRow(missingText, "Missing", 1); // update the Missing row
            SetRow(friedText, "Fried", 2); // update the Fried row
        }

        if (panel != null && panel != gameObject) panel.SetActive(all > 0); // hide the panel if there are no tasks

        if (complete && !allDone) // just finished everything
        {
            allDone = true; // remember it is done
            onAllComplete.Invoke(); // run the event once
        }
        else if (!complete) // not finished
        {
            allDone = false; // reset the flag
        }
    }

    void SetRow(GameObject row, string label, int i) // updates one task row
    {
        if (row == null) return; // no row, do nothing

        row.SetActive(total[i] > 0); // hide the row if this type has no tasks
        if (total[i] == 0) return; // stop if there are none

        bool done = fixedCount[i] >= total[i]; // true if all of this type are fixed
        string message = label + "  " + fixedCount[i] + " / " + total[i] + (done ? "  DONE" : ""); // build the row words
        SetLabel(row, message, done ? doneColor : pendingColor); // show it in green if done
    }
}