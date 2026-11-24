using System.Collections.Generic; // Gives access to HashSet and IEnumerable
using UnityEngine; // Gives access to Unity classes like MonoBehaviour, Object, Debug
using UnityEngine.Events; // Gives access to UnityEvent

public class MazeTaskGroup : MonoBehaviour // Keeps track of one type of task and how many items are done
{
    public string taskName = "Task"; // The name of this task type (Bent, MissingParts or Fried)
    public UnityEvent onAllCompleted = new UnityEvent(); // Fires once when every item is done

    readonly HashSet<Object> registered = new HashSet<Object>(); // Every item that belongs to this task
    readonly HashSet<Object> completed = new HashSet<Object>(); // Every item that is already done
    bool fired; // True once the completion event was fired

    public int Total => registered.Count; // How many items exist
    public int Done => completed.Count; // How many items are done
    public bool IsComplete => registered.Count > 0 && completed.Count >= registered.Count; // True when every item is done

    public void Register(Object item) => registered.Add(item); // Adds an item to this task

    public IEnumerable<Object> Items => registered; // Lets the compass see every item
    public bool IsItemDone(Object item) => completed.Contains(item); // True if this item is already fixed

    public void MarkComplete(Object item) // Marks one item as done
    {
        if (!registered.Contains(item)) registered.Add(item); // Add the item first if it was not registered
        if (!completed.Add(item)) return; // Stop if it was already marked as done

        Debug.Log($"[{taskName}] {Done}/{Total}"); // Print the progress in the Console

        ReportToObjectives(); // Tell the quest panel one more item is fixed

        if (!fired && IsComplete) // If every item is done and we did not fire yet
        {
            fired = true; // Remember that we fired
            Debug.Log($"[{taskName}] COMPLETE"); // Print a message in the Console
            onAllCompleted?.Invoke(); // Fire the completion event
        }
    }

    void ReportToObjectives() // Tells the right row of the quest panel that an item is fixed
    {
        if (ObjectivesUI.Instance == null) return; // Stop if there is no quest panel

        if (taskName == "Bent") ObjectivesUI.Instance.ReportFixed(ObjectiveType.Bent); // Update the Bent row
        else if (taskName == "MissingParts") ObjectivesUI.Instance.ReportFixed(ObjectiveType.Missing); // Update the Missing row
        else if (taskName == "Fried") ObjectivesUI.Instance.ReportFixed(ObjectiveType.Fried); // Update the Fried row
    }
}